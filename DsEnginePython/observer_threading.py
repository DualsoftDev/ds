from abc import ABC, abstractmethod
from typing import List
import threading

class IObserver(ABC):
    @abstractmethod
    def update(self, _id, _onoff_status):
        pass

class Observable:
    def __init__(self):
        self.observers: List[IObserver] = []
        self.lock = threading.Lock()

    def attach(self, observer:IObserver):
        with self.lock:
            self.observers.append(observer)

    def detach(self, observer:IObserver):
        with self.lock:
            self.observers.remove(observer)

    def call_update(self, _observer:IObserver, _id, _onoff_status):
        _observer.update(_id, _onoff_status)

    def notify(self, _id, _onoff_status):
        with self.lock:
            observers_copy = self.observers[:]

        for observer in observers_copy:
            observer.update(_id, _onoff_status)