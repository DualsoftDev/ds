:smile: Welcome to the DS world  :smile:
# Example 3

## Automatic door system :door:


 ![AAA](./png/ex3.dio.png)
 
  - action list 
    1. Open
    2. Close

```
 [Sys]door  = { Open <||> Close;
               (Detect) > Open, @10sec  > Close;
               @10sec <| Close
  }
```
