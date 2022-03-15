$\underbrace{
\overbrace{ b_1 b_2 \cdots  b_r}^{r} \overbrace{b_{r+1} \cdots b_n}^{n-r}
}_{n}   $
- segment 갯수
    - $n$ : 전체
    - $r$ : 초기 상태 고정
    - $n-r$ : 초기 상태 don't care

- 시작 가능 상태 경우의 수 : $k=2^{n-r}$
    case 1 : $b_1 b_2 \cdots b_r \overbrace{0 \cdots 0 0}^{n-r}$
    case 2 : $b_1 b_2 \cdots b_r 0 \cdots 0 1$
    case 3 : $b_1 b_2 \cdots b_r 0 \cdots 1 0$
    case 4 : $b_1 b_2 \cdots b_r 0 \cdots 1 1$
    $\cdots$
    case $k$ : $b_1 b_2 \cdots b_r 1 \cdots 1 1$

- 상태(2진)의 값 변환:
    - 2진 상태값 원주율 mapping function: $\phi$
    $\varphi_{0_i} = \phi(case_i) = \mathit{case}_i / 2^n \times 2\pi$

- 시작점 값 ($k$ 개): $ 1 \le i \le k$
    $\varphi_{0_i} = \phi(case_i) = \mathit{case}_i / 2^n \times 2\pi$

- $\varphi_\theta : \theta$ 진행 중의 $\varphi$ 값
    - $\mathit{state}_\theta = b_1 b_2 \cdots  b_n | \theta$ : $\theta$ 만큼 진행한 상태에서의 모든 child segment 의 상태 값

    - $\mathit{c\_state}_\theta = [b'_1 b'_2 \cdots  b'_n ]_\theta$
        - casality $C_{i, \theta}$ 를 고려한 state 값        

        where
                $b'_i = 0$ for non causal
                $b'_i = b_i$ for causal
    $\varphi_\theta = \phi(\mathit{c\_state}_\theta)$ 
