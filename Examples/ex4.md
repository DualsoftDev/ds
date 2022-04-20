:smile: Welcome to the DS world  :smile:
# Example 4

## Traffic light system :traffic_light:


 ![AAA](./png/ex4.dio.png)
 
  - action list 
    1. RedLight
    2. GreenLight
    3. Walk


```
     [sys]trafficlight  = { RedLight <| (Button) > @selfr (Walk);
                            RedLight < _RisingRun;
          Walk = { GreenLight > @s(30) > RedLight };
     }
```
