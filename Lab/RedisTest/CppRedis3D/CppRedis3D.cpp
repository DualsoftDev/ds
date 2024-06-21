/*
 - hiredis ���̺귯���� �⺻������ ������ �������� ������, ���� �����忡�� ���ÿ� ���� �� �����ϴ�. 
   �̸� �ذ��ϱ� ���� �� ���� ���� Redis ������ ����Ͽ� �ϳ��� ������, �ٸ� �ϳ��� ���࿡ ���.
*/

#include <iostream>
#include <hiredis/hiredis.h>
#include <string>
#include <thread>

// ������ ä�� �� ������ ä�� ����
std::string subscribeChannel = "d2g";
std::string publishChannel = "g2d";


// Redis ������ �����ϴ� �Լ�
redisContext* connectToRedis(const std::string& hostname, int port) {
    redisContext* context = redisConnect(hostname.c_str(), port);
    if (context == NULL || context->err) {
        if (context) {
            std::cerr << "Connection error: " << context->errstr << std::endl;
            redisFree(context);
        }
        else {
            std::cerr << "Connection error: can't allocate redis context" << std::endl;
        }
        return NULL;
    }
    return context;
}

// �޽����� �����ϰ� �������ϴ� �Լ�
void handleMessage(redisContext* publishContext, const std::string& message) {
    std::cout << "Received [" << message << "] from channel " << subscribeChannel << std::endl;

    // ���� �޽����� producer���� ������
    redisCommand(publishContext, "PUBLISH %s %s", publishChannel.c_str(), message.c_str());
    std::cout << "Sent back [" << message << "] to channel " << publishChannel << std::endl;
}

// ������ ó���ϴ� �Լ�
void subscribeThread(redisContext* subscribeContext, redisContext* publishContext) {
    redisReply* reply;
    reply = (redisReply*)redisCommand(subscribeContext, "SUBSCRIBE %s", subscribeChannel.c_str());
    freeReplyObject(reply);

    while (redisGetReply(subscribeContext, (void**)&reply) == REDIS_OK) {
        if (reply->type == REDIS_REPLY_ARRAY && reply->elements == 3) {
            std::string message_type = reply->element[0]->str;
            std::string channel = reply->element[1]->str;
            std::string message = reply->element[2]->str;

            if (message_type == "message" && channel == subscribeChannel) {
                handleMessage(publishContext, message);
            }
        }
        freeReplyObject(reply);
    }
}

int main() {
    // Redis ������ ����
    redisContext* subscribeContext = connectToRedis("127.0.0.1", 6379);
    if (subscribeContext == NULL) {
        return 1;
    }

    redisContext* publishContext = connectToRedis("127.0.0.1", 6379);
    if (publishContext == NULL) {
        redisFree(subscribeContext);
        return 1;
    }

    // ������ ó���ϴ� ������ ����
    std::thread subThread(subscribeThread, subscribeContext, publishContext);

    // ���� �����忡�� �޽��� ���� test
    redisCommand(publishContext, "PUBLISH %s %s", publishChannel.c_str(), "HELO");

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
