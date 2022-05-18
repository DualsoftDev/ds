from kafka import KafkaProducer
from json import dumps

producer = KafkaProducer(
    bootstrap_servers = ['localhost:9092'],
    acks = 0,
    compression_type = 'gzip',
    value_serializer = lambda x : dumps(x).encode('utf-8')
)

def send_data(data):
    producer.send('test', value = data)
    producer.flush()