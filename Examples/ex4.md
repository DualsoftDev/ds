:smile: Welcome to the DS world  :smile:
# Example 4

## Traffic light system :traffic_light:


 ![AAA](./png/ex4.dio.png)
 
  - action list 
    1. RedLight
    2. GreenLight
    3. Walk


```
     [sys]trafficlight  = {
          [task] t = { Button; }
          [flow] f1 = {
               _RisingRun > RedLight <| #(Button) > @selfr (Walk);
               Walk < #(Button)
               Walk = { GreenLight > @s(30) > RedLight };
          }
     }
```
