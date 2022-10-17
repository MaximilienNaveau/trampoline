#! /usr/env/bin python

from time import sleep
import pygame

if __name__ == "__main__":
    clock = pygame.time.get_ticks()
    time1 = clock.tick()
    sleep(1)
    time2 = clock.get_time()
    sleep(2)
    time3 = clock.get_time()
    print("time1 = ", time1)
    print("time2 = ", time2)
    print("time3 = ", time3)
    print("time2 - time1 = ", time2 - time1)
    print("time3 - time2 = ", time3 - time2)