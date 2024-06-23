#include <SimpleAmqpClient/SimpleAmqpClient.h>
#include <iostream>
#include <string>

using namespace AmqpClient;

// CppRmq3D.cpp : Defines the entry point for the application.
//

#include "CppRmq3D.h"

using namespace std;

int main()
{
    std::string queue_name = "toConsumer";
    std::string host = "localhost";

    try {
        // Create a connection to the RabbitMQ server
        Channel::ptr_t channel = Channel::Create(host);

        // Declare a queue to consume messages from
        channel->DeclareQueue(queue_name, false, true, false, false);

        // Create a consumer tag
        std::string consumer_tag = channel->BasicConsume(queue_name, "");

        std::cout << "Consumer started. Waiting for messages..." << std::endl;

        // Consume messages in a loop
        while (true) {
            Envelope::ptr_t envelope = channel->BasicConsumeMessage(consumer_tag);
            std::string message_body = envelope->Message()->Body();

            std::cout << " [x] Received: " << message_body << std::endl;

            // Simulate processing
            if (message_body == "exit") {
                break;
            }
        }
    }
    catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << std::endl;
        return 1;
    }

    return 0;
}
