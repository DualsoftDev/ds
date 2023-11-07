#pragma once

// VC++ 기준: 
// vcpkg.exe install cppzmq
// vcpkg.exe integrate install

#include <zmq.hpp>
//#include <zmq.h>
#include <string>
#include <iostream>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <queue>
#include <atomic>
#include <future>
#include <vector>
#include <cstring>
#include <map>

class Client {
private:
    std::atomic_bool done{ false };
    zmq::context_t context{ 1 };
    zmq::socket_t socket{ context, zmq::socket_type::dealer };
    std::mutex queue_mutex;
    std::condition_variable queue_cond;
    std::queue<zmq::message_t> message_queue;

    void WorkerThread() {
        while (!done) {
            zmq::message_t message;
            if (socket.recv(message, zmq::recv_flags::none)) {
                std::lock_guard<std::mutex> lock(queue_mutex);
                message_queue.push(std::move(message));
                queue_cond.notify_one();
            }
            else {
                std::this_thread::sleep_for(std::chrono::milliseconds(100));
            }
        }
    }

public:
    Client(const std::string& server_address) {
        socket.connect(server_address);

        // Start the worker thread
        std::thread worker(&Client::WorkerThread, this);
        worker.detach();
    }

    ~Client() {
        done = true;
        socket.close();
    }

    void SendRequest(const std::string& request) {
        zmq::message_t message(request.data(), request.size());
        socket.send(message, zmq::send_flags::none);
    }

    std::string ReceiveReply() {
        std::unique_lock<std::mutex> lock(queue_mutex);
        queue_cond.wait(lock, [&] { return !message_queue.empty(); });

        auto msg = std::move(message_queue.front());
        message_queue.pop();

        return std::string(static_cast<char*>(msg.data()), msg.size());
    }

    // Other methods similar to F# implementation would go here
};


