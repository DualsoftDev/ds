# OPC with node.js

- [기능 비교 테이블](https://dl.gi.de/bitstream/handle/20.500.12116/34742/C4-8.pdf?sequence=1&isAllowed=y)

## node-opcua
- [NodeOPCUA! the OPCUA sdk for node.js](https://node-opcua.github.io/)

## node-red
- [node-red-contrib-opc-da](https://flows.nodered.org/node/node-red-contrib-opc-da)
node-red-contrib-opc-da is an OPC-DA compatible node for Node-RED that allow interaction with remote OPC-DA servers. __Currently only reading and browsing operations are supported.__

- Node RED 입문서? [Node RED Programming Guide &#8211; Programming the IoT](http://noderedguide.com/)

- [node-red 와 SIMENS PLC 연결 방법 문서](https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=&ved=2ahUKEwjfisn4htL3AhWTpVYBHSq5B1gQFnoECAMQAQ&url=https%3A%2F%2Fwww.automation.siemens.com%2Fsce-static%2Flearning-training-documents%2Ftia-portal%2Fadvanced-communication%2Fsce-092-303-opc-ua-s7-1500-node-red-en.docx&usg=AOvVaw3zLvOW6VdVMRDTZwjMvk4d)


- [Tutorial: Using OPC-UA with FRED (Cloud Node-RED) &#8211; Sensetecnic Developer &#8211; Hosted Node-RED](http://developers.sensetecnic.com/article/tutorial-using-opc-ua-with-fred/)


- Youtube 동영상 [OPC UA Lesson 2 - Starting with OPC UA Server simulation and OPC UA Client](https://www.youtube.com/watch?v=yCd2j2WsgBM&t=628s)



## python
- [GitHub - FreeOpcUa/opcua-asyncio: OPC UA library for python &gt;= 3.7](https://github.com/FreeOpcUa/opcua-asyncio)
    - [add / delete example](https://github.com/FreeOpcUa/opcua-asyncio/blob/master/examples/client_deleting.py)

- [client 에서 추가 삭제](https://github.com/FreeOpcUa/python-opcua/issues/719)
```
i got it. If you want to change or create some objects you should connect to server via admin

client = Client("opc.tcp://admin@localhost:4840/freeopcua/server/") #connect using a user
```


- KepServer 연동
    $ python client_to_kepware.py
    asyncua.ua.uaerrors._auto.BadSecurityChecksFailed: "An error occurred verifying security."(BadSecurityChecksFailed)

    - Security None 추가


    - Edit > Properties > Opc UA > client session
        - allow anonymous login : YES 로 변경 하니 동작함..



- [node-opcua 로 kepserver 연동시, certificate 없어 에러나는 경우](https://stackoverflow.com/questions/64440584/nodejs-how-to-generate-certificate-and-private-key-with-node-opcua-pki)
```
cd ~/tmp/cert
npm install rimraf
npm install node-opcua-pki certificates
npx node-opcua-pki certificate -o client_certificate.pem
```

- [Kepserver: endpoint가 존재하지 않는다는 에러가 발생하는 경우 - opchub 문서 참조](http://opchub.com/download/KepserverEX_OPC_UA_Configuration.pdf)

    - kepserver 트레이 아이콘 우클릭 후 OPC UA configuration 에서 opc.tcp://127.0.0.1:49320의 security 설정에 None 추가

    - kepserver 트레이 아이콘 우클릭 후 Reinitialize 로 적용


node-opcua.client --> KEPServer : OK!
    // URL 주소: 뒷부분 path 를 없애 주어야 연결 됨.
    const endpointUrl = "opc.tcp://127.0.0.1:49320";
    const nodeId = "ns=2;s=Channel1.Device1.Tag1";

python.client --> KEPServer : OK!
    // URL 주소: 뒷부분 path 포함.
    url = "opc.tcp://localhost:49320/OPCUA/SimulationServer/"



