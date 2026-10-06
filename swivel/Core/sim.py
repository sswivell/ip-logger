"""Fake-but-plausible traffic series driving the live charts."""

import random
import time

N = 90


class Sim:
    """Random-walk CPU, network and latency series, advanced once a frame."""

    def __init__(self):
        self.cpu_t = self.cpu = random.uniform(10, 40)
        self.dn_t = self.dn = random.uniform(3, 18)
        self.up_t = self.up = random.uniform(.5, 6)
        self.latency = 42.0
        self.cpu_hist = [random.uniform(10, 40) for _ in range(N)]
        self.dn_hist = [random.uniform(3, 18) for _ in range(N)]
        self.up_hist = [random.uniform(.5, 6) for _ in range(N)]
        self.t0 = time.time()

    def step(self, dt):
        """Nudge every series toward its target and record a new sample."""
        if random.random() < .05:
            self.cpu_t = random.uniform(5, 95)
        self.cpu = max(2, min(99, self.cpu
                              + (self.cpu_t - self.cpu) * .1
                              + random.uniform(-1.8, 1.8)))
        self.cpu_hist.append(self.cpu)

        if random.random() < .1:
            self.dn_t = random.uniform(.5, 60)
            self.up_t = random.uniform(.2, 18)
        self.dn = max(.1, min(80, self.dn
                              + (self.dn_t - self.dn) * .15
                              + random.uniform(-2, 2)))
        self.up = max(.05, min(30, self.up
                               + (self.up_t - self.up) * .15
                               + random.uniform(-.8, .8)))
        self.dn_hist.append(self.dn)
        self.up_hist.append(self.up)

        self.latency = max(12, min(180, self.latency
                                   + random.uniform(-3, 3)))
        return dt