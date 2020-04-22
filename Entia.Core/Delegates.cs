namespace Entia.Core
{
    public delegate bool TryFunc<TIn, TOut>(TIn input, out TOut output);
    public delegate bool TryFunc<TIn1, TIn2, TOut>(TIn1 input, TIn2 inpu2, out TOut output);
    public delegate bool TryInFunc<TIn, TOut>(in TIn input, out TOut output);
    public delegate bool TryInFunc<TIn1, TIn2, TOut>(in TIn1 input1, in TIn2 input2, out TOut output);
    public delegate bool TryRefFunc<TIn, TOut>(ref TIn input, out TOut output);
    public delegate bool TryRefFunc<TIn1, TIn2, TOut>(ref TIn1 input1, ref TIn2 input2, out TOut output);
    public delegate TOut InFunc<TIn, TOut>(in TIn input);
    public delegate TOut InFunc<TIn1, TIn2, TOut>(in TIn1 input1, in TIn2 input2);
    public delegate TOut RefFunc<TIn, TOut>(ref TIn input);
    public delegate TOut RefFunc<TIn1, TIn2, TOut>(ref TIn1 input1, ref TIn2 input2);
    public delegate TOut RefInFunc<TIn1, TIn2, TOut>(ref TIn1 input1, in TIn2 input2);
    public delegate void InAction<T>(in T input);
    public delegate void InAction<T1, T2>(in T1 input1, in T2 input2);
    public delegate void InAction<T1, T2, T3>(in T1 input1, in T2 input2, in T3 input3);
    public delegate void RefAction<T>(ref T input);
    public delegate void RefAction<T1, T2>(ref T1 input1, ref T2 input2);
    public delegate void RefAction<T1, T2, T3>(ref T1 input1, ref T2 input2, ref T3 input3);
    public delegate void RefAction<T1, T2, T3, T4>(ref T1 input1, ref T2 input2, ref T3 input3, ref T4 input4);
    public delegate void RefAction<T1, T2, T3, T4, T5>(ref T1 input1, ref T2 input2, ref T3 input3, ref T4 input4, ref T5 input5);
    public delegate void RefAction<T1, T2, T3, T4, T5, T6>(ref T1 input1, ref T2 input2, ref T3 input3, ref T4 input4, ref T5 input5, ref T6 input6);
    public delegate void InRefAction<T1, T2>(in T1 input1, ref T2 input2);
    public delegate void RefInAction<T1, T2>(ref T1 input1, in T2 input2);
}
