:smile: Welcome to the DS world  :smile:
# Example 2 

## coffee machine system :coffee:


 ![AAA](./png/ex2.dio.png)
 
  - action list 
     1. MakeCoffee
     

```
     [sys]coffee  = {
          [task] t = { PutCup; PushButton; GetCoffee; }
          [flow] f = {
                #(PutCup), #(PushButton) > MakeCoffee;
                MakeCoffee <| #(GetCoffee);
          }
     }
```
