#include <zmq.hpp>
#include <string>
#include <iostream>

int main()
{
    zmq::context_t context(1);
    zmq::socket_t socket(context, ZMQ_REQ);
    // WSL host 에서 서버가 구동 중이라면, WSL host 의 ip address 를 적어야 한다.
    socket.connect("tcp://192.168.9.2:5555");
    
    while (true) {
        std::cout << "Enter command(or 'q' to quit): ";
        std::string userInput;
        std::getline(std::cin, userInput);

        if (userInput == "q") {
            break;
        }

        auto len = userInput.length();
        zmq::message_t request(len + 1);
        memcpy(request.data(), userInput.c_str(), len + 1);
        std::cout << "Sending " << userInput << "..." << std::endl;
        socket.send(request, zmq::send_flags::none);

        zmq::message_t reply;
        auto result = socket.recv(reply, zmq::recv_flags::none);
        std::string result_str(static_cast<char*>(reply.data()), reply.size());
        auto detail = socket.recv(reply, zmq::recv_flags::none);
        std::string detail_str(static_cast<char*>(reply.data()), reply.size());
        std::cout << "Received: " << result_str << ", Detail: " << detail_str << std::endl;
    }

    return 0;
}
