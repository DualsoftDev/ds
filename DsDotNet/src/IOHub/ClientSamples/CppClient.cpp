#include <zmq.hpp>
#include <string>
#include <iostream>

int main()
{
    zmq::context_t context(1);
    zmq::socket_t client(context, ZMQ_DEALER);

    std::string identity = std::to_string(rand());
    client.set(zmq::sockopt::routing_id, identity);
    // WSL host 에서 서버가 구동 중이라면, WSL host 의 ip address 를 적어야 한다.
    client.connect("tcp://192.168.9.2:5555");

    // 서버에 등록
    std::string registerMsg = "REGISTER";
    zmq::message_t registerMessage(registerMsg.c_str(), registerMsg.size());
    client.send(registerMessage, zmq::send_flags::none);

    
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
        client.send(request, zmq::send_flags::none);
        std::cout << "Sent." << std::endl;

        zmq::message_t reply;
        auto result = client.recv(reply, zmq::recv_flags::none);
        std::string result_str(static_cast<char*>(reply.data()), reply.size());
        std::cout << "-- Received: " << result_str << std::endl;

        auto detail = client.recv(reply, zmq::recv_flags::none);
        std::string detail_str(static_cast<char*>(reply.data()), reply.size());
        std::cout << "Received: " << result_str << ", Detail: " << detail_str << std::endl;
    }

    return 0;
}
