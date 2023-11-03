#include <zmq.hpp>
#include <string>
#include <iostream>

zmq::message_t getNextId()
{
	static int id = 0;
	zmq::message_t reqId(&id, sizeof(id));
	id++;
	return reqId;
}
int main()
{
    zmq::context_t context(1);
    zmq::socket_t client(context, ZMQ_DEALER);
    // 현재 시간을 기반으로 시드를 설정합니다.
    srand(static_cast<unsigned int>(time(nullptr)));

    std::string identity = std::to_string(rand());
    client.set(zmq::sockopt::routing_id, identity);
    // WSL host 에서 서버가 구동 중이라면, WSL host 의 ip address 를 적어야 한다.
    client.connect("tcp://localhost:5555");

    int id = 0;
    // 서버에 등록
    std::string registerMsg = "REGISTER";
    zmq::message_t registerMessage(registerMsg.c_str(), registerMsg.size());
    client.send(getNextId(), zmq::send_flags::sndmore);
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
        client.send(getNextId(), zmq::send_flags::sndmore);
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
