/*
 - hiredis ���̺귯���� �⺻������ ������ �������� ������, ���� �����忡�� ���ÿ� ���� �� �����ϴ�. 
   �̸� �ذ��ϱ� ���� �� ���� ���� Redis ������ ����Ͽ� �ϳ��� ������, �ٸ� �ϳ��� ���࿡ ���.
*/
#include <iostream>
#include <hiredis/hiredis.h>
#include <string>
#include <thread>

// �޽����� �����ϰ� �������ϴ� �Լ�
void handleMessage(redisContext* publishContext, const std::string& publishChannel, const std::string& message) {
    std::cout << "Received message: " << message << " from channel: toConsumer" << std::endl;

    // ���� �޽����� producer���� ������
    redisCommand(publishContext, "PUBLISH %s %s", publishChannel.c_str(), message.c_str());
    std::cout << "Sent back message: " << message << " to channel: toProducer" << std::endl;
}

// ������ ó���ϴ� �Լ�
void subscribeThread(redisContext* subscribeContext, redisContext* publishContext, const std::string& subscribeChannel, const std::string& publishChannel) {
    redisReply* reply;
    reply = (redisReply*)redisCommand(subscribeContext, "SUBSCRIBE %s", subscribeChannel.c_str());
    freeReplyObject(reply);

    while (redisGetReply(subscribeContext, (void**)&reply) == REDIS_OK) {
        if (reply->type == REDIS_REPLY_ARRAY && reply->elements == 3) {
            std::string message_type = reply->element[0]->str;
            std::string channel = reply->element[1]->str;
            std::string message = reply->element[2]->str;

            if (message_type == "message" && channel == subscribeChannel) {
                handleMessage(publishContext, publishChannel, message);
            }
        }
        freeReplyObject(reply);
    }
}

int main() {
    // Redis ������ ���� (������)
    redisContext* subscribeContext = redisConnect("127.0.0.1", 6379);
    if (subscribeContext == NULL || subscribeContext->err) {
        if (subscribeContext) {
            std::cerr << "Connection error: " << subscribeContext->errstr << std::endl;
            redisFree(subscribeContext);
        }
        else {
            std::cerr << "Connection error: can't allocate redis context" << std::endl;
        }
        return 1;
    }

    // Redis ������ ���� (�����)
    redisContext* publishContext = redisConnect("127.0.0.1", 6379);
    if (publishContext == NULL || publishContext->err) {
        if (publishContext) {
            std::cerr << "Connection error: " << publishContext->errstr << std::endl;
            redisFree(publishContext);
        }
        else {
            std::cerr << "Connection error: can't allocate redis context" << std::endl;
        }
        return 1;
    }

    // ������ ä�� �� ������ ä�� ����
    std::string subscribeChannel = "toConsumer";
    std::string publishChannel = "toProducer";

    // ������ ó���ϴ� ������ ����
    std::thread subThread(subscribeThread, subscribeContext, publishContext, subscribeChannel, publishChannel);

    // ���� �����忡�� �޽��� ����
    std::this_thread::sleep_for(std::chrono::seconds(1)); // ��� ���
    redisCommand(publishContext, "PUBLISH %s %s", publishChannel.c_str(), "Hello, Producer!");

    // ���� �����尡 ����� ������ ���
    subThread.join();

    // ���� ����
    redisFree(subscribeContext);
    redisFree(publishContext);
    return 0;
}



//
//
//
//#include <iostream>
//#include <hiredis/hiredis.h>
//
//int main() {
//    // Redis ������ ����
//    redisContext* c = redisConnect("127.0.0.1", 6379);
//    if (c == NULL || c->err) {
//        if (c) {
//            std::cerr << "Error: " << c->errstr << std::endl;
//            redisFree(c);
//        }
//        else {
//            std::cerr << "Can't allocate redis context" << std::endl;
//        }
//        return 1;
//    }
//
//    // PING ��ɾ� ���� �� ���� Ȯ��
//    redisReply* reply = (redisReply*)redisCommand(c, "PING");
//    std::cout << "PING: " << reply->str << std::endl;
//    freeReplyObject(reply);
//
//    // SET ��ɾ�� Ű-�� ����
//    reply = (redisReply*)redisCommand(c, "SET %s %s", "foo", "bar");
//    std::cout << "SET: " << reply->str << std::endl;
//    freeReplyObject(reply);
//
//    // GET ��ɾ�� Ű�� �� ��������
//    reply = (redisReply*)redisCommand(c, "GET %s", "foo");
//    std::cout << "GET foo: " << reply->str << std::endl;
//    freeReplyObject(reply);
//
//    // ���� ����
//    redisFree(c);
//    return 0;
//}
