:smile: Welcome to the DS world  :smile:
# Example 3

## Automatic door system :door:


 ![AAA](./png/ex3.dio.png)
 
  - action list 
    1. Open
    2. Close
    3. Detecting

```
 [Sys]door  = { Open <|> Close
               Detecting > Open, CloseDelay(10Sec)  > Close
               CloseDelay(10Sec) <| Close
  }
```
