/*
 * 미완성 버젼.
 *   1. 메시지 전송과 별도의 thread 에서 구동되는 메시지 수신 확인까지만 구현
 *
 * 추후 필요한 작업 내용
 *   1. 메시지 수신 루프를 별도의 스레드에서 실행하도록 수정
 *   1. 메시지 수신 루프에서 메시지를 받으면, Queue 에 넣도록 수정
 *   1. Client 메인 스레드에서 서버에 메시지 전송 후, 메시지 수신 루프에서 넣은 Queue 값과 비교하여 처리 (message id 이용)
 *   1. 메시지 수신 루프에서 서버에서 전송된 notify message 처리.
 */

#include <zmq.hpp>
#include <string>
#include <iostream>

#include <thread>
#include <atomic>
#include <chrono>
#include <vector>

zmq::message_t getNextId()
{
	static int id = 0;
	zmq::message_t reqId(&id, sizeof(id));
	id++;
	return reqId;
}


bool tryReceiveMultipartMessage(zmq::socket_t& socket, std::vector<zmq::message_t>& messages) {
    zmq::message_t message;
    while (true) {
        if (!socket.recv(message, zmq::recv_flags::dontwait)) {
            // 메시지를 받지 못하면 false를 반환합니다.
            return false;
        }
        // 메시지를 받았으므로 vector에 추가합니다.
        messages.push_back(std::move(message));

        // 마지막 메시지인지 체크합니다.
        if (!message.more()) {
            break;
        }
    }
    // 모든 메시지 파트를 성공적으로 받았으므로 true를 반환합니다.
    return true;
}


std::atomic_bool cancelled(false);
void receiveMessageLoop(zmq::socket_t& client) {
    while (!cancelled) {

        std::vector<zmq::message_t> messages;
        if (tryReceiveMultipartMessage(client, messages)) {
            // 메시지 처리 로직
            zmq::message_t &m0 = messages[0];
            std::string requestStr(static_cast<char*>(m0.data()), m0.size());
            // 여기서 메시지를 처리합니다.
            std::cout << "Received message: " << m0 << std::endl;

            // 요청에 따라 처리 로직을 구현합니다.
            // ...
        }
        else {
			std::this_thread::sleep_for(std::chrono::milliseconds(100));
		}
    }
}



int main()
{
    zmq::context_t context(1);
    zmq::socket_t client(context, ZMQ_DEALER);
    // 현재 시간을 기반으로 시드를 설정합니다.
    srand(static_cast<unsigned int>(time(nullptr)));

    std::string identity = std::to_string(rand());
    client.set(zmq::sockopt::routing_id, identity);
    // WSL linux 에서 client 가 구동 중이고, WSL host 에서 서버가 구동 중이라면,
    // "localhost" 로 적을 수 없다.  (wsl 의 localhost 와 WSL host 의 localhost 는 서로 다르다.)
    // wsl host 의 ip address 를 적어야 한다.
    client.connect("tcp://192.168.9.2:5555");

    // 메시지 수신 루프를 별도의 스레드에서 실행합니다.
    std::thread worker(receiveMessageLoop, std::ref(client));


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

        //zmq::message_t reply;
        //auto result = client.recv(reply, zmq::recv_flags::none);
        //std::string result_str(static_cast<char*>(reply.data()), reply.size());
        //std::cout << "-- Received: " << result_str << std::endl;

        //auto detail = client.recv(reply, zmq::recv_flags::none);
        //std::string detail_str(static_cast<char*>(reply.data()), reply.size());
        //std::cout << "Received: " << result_str << ", Detail: " << detail_str << std::endl;
    }

    return 0;
}
