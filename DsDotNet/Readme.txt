Logix5000 conversion

- https://www.dmcinfo.com/latest-thinking/blog/id/9894/writing-rockwell-ladder-logic-in-xml
- https://kiptr.com/article1_1


XML(*.L5X) 이용해서

- dualsoft.snk
  # sn -k dualsoft.snk 를 통해 생성
  sn -p dualsoft.snk dualsoftPublic.snk     # 공개키 생성
  sn -tp dualsoftPublic.snk                 # 공개키 추출
F:\Git\ds\DsDotNet>sn -tp dualsoftPublic.snk

Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.0
Copyright (c) Microsoft Corporation. All rights reserved.

공개 키(해시 알고리즘: sha1):
0024000004800000940000000602000000240000525341310004000001000100855bad84d588e7
de3f5f42fc5cbede2a86f6e5719177dd89ac5acff32c4ddf0fc646cdfc487daed9578addbf403b
0e89b9efa628185a44af0d48667ab990c5a977544fbedef85764883d6654d0c01340d69846d14c
a838e0b69fd0d4c5a7d851240aa568bf0b1a2987758f60ad8741a479510b5b8bf45710e78aebd0
e32795ba

공개 키 토큰은 6b5d64113b30ff1b입니다.




- https://literature.rockwellautomation.com/idc/groups/literature/documents/wp/logix-wp005_-en-p.pdf