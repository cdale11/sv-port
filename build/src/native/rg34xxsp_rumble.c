/* SPDX-License-Identifier: MIT
 * KNULLI H700 motor bridge. The pipe lifetime follows the game process;
 * EOF, signals and a 250 ms command timeout all turn the motor off.
 */
#define _POSIX_C_SOURCE 200809L
#include <errno.h>
#include <fcntl.h>
#include <poll.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

static volatile sig_atomic_t stopping;
static int motor = -1;
static int state = -1;
static void stop(int sig) { (void)sig; stopping = 1; }
static int set_motor(int on) {
    if (state == on) return 0;
    if (lseek(motor, 0, SEEK_SET) < 0 || write(motor, on ? "1\n" : "0\n", 2) != 2)
        return -1;
    state = on;
    return 0;
}

int main(int argc, char **argv) {
    const char *path = getenv("SDV_RUMBLE_DEVICE");
    if (!path || !*path) path = "/sys/class/power_supply/axp2202-battery/moto";
    if (argc > 2 || (argc == 2 && strcmp(argv[1], "--test") && strcmp(argv[1], "--probe"))) {
        fprintf(stderr, "Usage: %s [--probe|--test]\n", argv[0]);
        return 2;
    }
    struct sigaction action = {0};
    action.sa_handler = stop;
    sigaction(SIGINT, &action, NULL);
    sigaction(SIGTERM, &action, NULL);
    sigaction(SIGHUP, &action, NULL);
    signal(SIGPIPE, SIG_IGN);
    motor = open(path, O_WRONLY | O_CLOEXEC);
    if (motor < 0 || set_motor(0) < 0) {
        fprintf(stderr, "RG34XXSP rumble unavailable: %s: %s\n", path, strerror(errno));
        puts("UNAVAILABLE");
        if (motor >= 0) close(motor);
        return 1;
    }
    if (argc == 2) {
        int result = 0;
        if (!strcmp(argv[1], "--test")) {
            result = set_motor(1);
            poll(NULL, 0, 150);
            if (set_motor(0) < 0) result = -1;
        }
        close(motor);
        return result < 0;
    }
    puts("READY");
    fflush(stdout);
    struct pollfd input = { STDIN_FILENO, POLLIN, 0 };
    int result = 0;
    while (!stopping) {
        int ready = poll(&input, 1, 250);
        if (ready < 0) {
            if (errno == EINTR) continue;
            result = 1;
            break;
        }
        if (!ready) {
            if (set_motor(0) < 0) { result = 1; break; }
            continue;
        }
        char commands[256];
        ssize_t count = read(STDIN_FILENO, commands, sizeof(commands));
        if (count <= 0) break;
        /* One byte per command; unknown data fails closed. */
        for (ssize_t i = 0; i < count; ++i) {
            if (commands[i] != '0' && commands[i] != '1') { result = 1; stopping = 1; break; }
            if (set_motor(commands[i] == '1') < 0) { result = 1; stopping = 1; break; }
        }
    }
    if (set_motor(0) < 0) result = 1;
    close(motor);
    return result;
}
