#include <zmq.hpp>
#include <string>
#include <iostream>

int main()
{
    zmq::context_t context(1);
    zmq::socket_t socket(context, ZMQ_REQ);
    // WSL host 에서 서버가 구동 중이라면, WSL host 의 ip address 를 적어야 한다.
    socket.connect("tcp://192.168.9.2:5555");
    std::string req = "read Mw100 Mx30 Md1234";

    auto len = req.length();
    zmq::message_t request(len+1);
    memcpy(request.data(), req.c_str(), len+1);
    std::cout << "Sending " << req << "..." << std::endl;
	socket.send(request, zmq::send_flags::none);

    zmq::message_t reply;
    auto _ = socket.recv(reply, zmq::recv_flags::none);
    std::string reply_str(static_cast<char*>(reply.data()), reply.size());
    std::cout << "Received: " << reply_str << std::endl;

    return 0;
}

