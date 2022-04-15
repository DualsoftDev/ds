:smile: Welcome to the DS world  :smile:
# Example 1 

## Light bulb system :bulb:


 ![AAA](./ex1.dio.png)
 
  - action list 
    1. ON
    1. OFF
    1. Light
    


```
     /* DS Language Unit
          1. '>'  is Start Edge
          2. '|>' is Reset Edge
          3. Action value(True/False) is End Sensing
     */

     //Light bulb system 
     [Sys]Light bulb  = {ON > LightOn <| OFF}
```

- DS Segment(action)  interface    

| interface | Desc | 
|:--:|:--:
|Start In|:fist:→:point_up:|
|Reset In|:fist:→:metal:|
|End Out|:fist:→:thumbsup:|
