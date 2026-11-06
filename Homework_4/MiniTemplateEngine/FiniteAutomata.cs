using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace MiniTemplateEngine;

internal enum State
{
    First,
    Second,
    Third,
    Fourth,
    Fifth,
    Sixth,
    Seventh,
    Eighth,
    Accepted,
    TemplateError
}
internal class OperandReader
{
    public State CurrentState {get; private set;} = State.First;

    public string ProcessWord(string word)
    {
        var result = new StringBuilder();
        foreach (var c in word)
        {
            ProcessSymbol(c);
            if (CurrentState == State.Accepted)
                break;
            result.Append(c);
        }
        if (!IsAccepting(CurrentState))
            throw new TemplateRenderException("operand not found");
        return result.ToString();
    }
    public void ProcessSymbol(char c)
    {
        switch (CurrentState)
        {
            case State.First:
            CurrentState = SwitchFromFirst(c);
            break;

            case State.Second:
            CurrentState = SwitchFromSecond(c);
            break;

            case State.Third:
            CurrentState = SwitchFromThird(c);
            break;

            case State.Fourth:
            CurrentState = SwitchFromFourth(c);
            break;

            case State.Fifth:
            CurrentState = SwitchFromFifth(c);
            break;

            case State.Sixth:
            CurrentState = SwitchFromSixth(c);
            break;

            case State.Seventh:
            CurrentState = SwitchFromSeventh(c);
            break;

            case State.Eighth:
            CurrentState = SwitchFromEighth(c);
            break;

            case State.Accepted:
            CurrentState = State.Accepted;
            break;

            case State.TemplateError:
            throw new TemplateRenderException("");
        }
    }

    #region функции перехода
    private 
    State SwitchFromFirst(char c)
    {
        switch (c)
        {
            case ' ' or '\t' or '\r' or '\n':
                return State.First;

            case '"':
                return State.Second;

            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_':
                return State.Fourth;

            case >= '0' and <= '9':
                return State.Sixth;

            default:
                if (char.IsWhiteSpace(c))
                    return State.First;
                else if (char.IsLetter(c))
                    return State.Fourth;
                else    
                    return State.TemplateError;
        }
    }

    private State SwitchFromSecond(char c)
    {
        switch (c)
        {
            case '"':
                return State.Third;

            case not '"':
                return State.Second;
        }
    }

    private State SwitchFromThird(char c)
    {
        return c switch
        {
            '"' => State.Second,
            _ => State.Accepted,
        };
    }

    private State SwitchFromFourth(char c)
    {
        switch (c)
        {
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_':
                return State.Fourth;

            case >= '0' and <= '9':
                return State.Fourth;
            
            case '.':
                return State.Fifth;

            default:
                if (char.IsLetter(c))
                    return State.Fourth;
                else    
                    return State.Accepted;
        }
    }

    private State SwitchFromFifth(char c)
    {
        switch (c)
        {
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_':
                return State.Fourth;

            default:
                if (char.IsLetter(c))
                    return State.Fourth;
                else
                    return State.TemplateError;
        }
    }

    private State SwitchFromSixth(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => State.Sixth,
            '.' => State.Seventh,
            _ => State.Accepted,
        };
    }

    private State SwitchFromSeventh(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => State.Eighth,
            _ => State.TemplateError,
        };
    }

    private State SwitchFromEighth(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => State.Eighth,
            _ => State.Accepted,
        };
    }
    #endregion

    private static bool IsAccepting(State state)
    {
        return state switch
        {
            State.First => false,
            State.Second => false,
            State.Third => true,
            State.Fourth => true,
            State.Fifth => false,
            State.Sixth => true,
            State.Seventh => false,
            State.Eighth => true,
            State.Accepted => true,
            _ => false,
        };
    }
}
