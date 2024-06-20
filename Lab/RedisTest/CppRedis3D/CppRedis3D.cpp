/*
 - hiredis 라이브러리는 기본적으로 스레드 안전하지 않으며, 여러 스레드에서 동시에 사용될 수 없습니다. 
   이를 해결하기 위해 두 개의 별도 Redis 연결을 사용하여 하나는 구독에, 다른 하나는 발행에 사용.
*/
#include <iostream>
#include <hiredis/hiredis.h>
#include <string>
#include <thread>

// 메시지를 수신하고 재전송하는 함수
void handleMessage(redisContext* publishContext, const std::string& publishChannel, const std::string& message) {
    std::cout << "Received message: " << message << " from channel: toConsumer" << std::endl;

    // 동일 메시지를 producer에게 재전송
    redisCommand(publishContext, "PUBLISH %s %s", publishChannel.c_str(), message.c_str());
    std::cout << "Sent back message: " << message << " to channel: toProducer" << std::endl;
}

// 구독을 처리하는 함수
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
    // Redis 서버에 연결 (구독용)
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

    // Redis 서버에 연결 (발행용)
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

    // 구독할 채널 및 발행할 채널 설정
    std::string subscribeChannel = "toConsumer";
    std::string publishChannel = "toProducer";

    // 구독을 처리하는 쓰레드 생성
    std::thread subThread(subscribeThread, subscribeContext, publishContext, subscribeChannel, publishChannel);

    // 메인 쓰레드에서 메시지 발행
    std::this_thread::sleep_for(std::chrono::seconds(1)); // 잠시 대기
    redisCommand(publishContext, "PUBLISH %s %s", publishChannel.c_str(), "Hello, Producer!");

    // 구독 쓰레드가 종료될 때까지 대기
    subThread.join();

    // 연결 해제
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
//    // Redis 서버에 연결
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
//    // PING 명령어 전송 및 응답 확인
//    redisReply* reply = (redisReply*)redisCommand(c, "PING");
//    std::cout << "PING: " << reply->str << std::endl;
//    freeReplyObject(reply);
//
//    // SET 명령어로 키-값 저장
//    reply = (redisReply*)redisCommand(c, "SET %s %s", "foo", "bar");
//    std::cout << "SET: " << reply->str << std::endl;
//    freeReplyObject(reply);
//
//    // GET 명령어로 키의 값 가져오기
//    reply = (redisReply*)redisCommand(c, "GET %s", "foo");
//    std::cout << "GET foo: " << reply->str << std::endl;
//    freeReplyObject(reply);
//
//    // 연결 해제
//    redisFree(c);
//    return 0;
//}
