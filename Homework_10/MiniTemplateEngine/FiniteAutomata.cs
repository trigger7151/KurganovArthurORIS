using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace MiniTemplateEngine;

/// <summary>
/// Состояния автомата чтения операнда.
/// </summary>
internal enum State
{
    /// <summary>Начало: пропуск пробелов, ожидание первого символа операнда.</summary>
    First,
    /// <summary>Внутри строки в кавычках.</summary>
    Second,
    /// <summary>После закрывающей кавычки (две кавычки подряд — кавычка внутри строки).</summary>
    Third,
    /// <summary>Внутри имени или пути (буквы, цифры, подчёркивание).</summary>
    Fourth,
    /// <summary>После точки в пути: ожидается следующее имя.</summary>
    Fifth,
    /// <summary>Целая часть числа.</summary>
    Sixth,
    /// <summary>После десятичной точки: ожидается цифра.</summary>
    Seventh,
    /// <summary>Дробная часть числа.</summary>
    Eighth,
    /// <summary>Операнд прочитан.</summary>
    Accepted,
    /// <summary>Недопустимый символ: ошибка в шаблоне.</summary>
    TemplateError
}
/// <summary>
/// Конечный автомат, читающий один операнд условия: строку в кавычках, число или путь к значению модели.
/// </summary>
internal class OperandReader
{
    /// <summary>
    /// Текущее состояние автомата.
    /// </summary>
    public State CurrentState {get; private set;} = State.First;

    /// <summary>
    /// Читает операнд с начала строки до символа, на котором автомат его принимает.
    /// </summary>
    /// <param name="word">Строка, начинающаяся с операнда.</param>
    /// <returns>Прочитанная часть строки (без символа, завершившего операнд).</returns>
    /// <exception cref="TemplateRenderException">Строка закончилась раньше, чем операнд был принят.</exception>
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
    /// <summary>
    /// Выполняет переход автомата по одному символу.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <exception cref="TemplateRenderException">Автомат находится в состоянии ошибки.</exception>
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
    /// <summary>
    /// Переход из начального состояния.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
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

    /// <summary>
    /// Переход внутри строки в кавычках.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
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

    /// <summary>
    /// Переход после закрывающей кавычки.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
    private State SwitchFromThird(char c)
    {
        return c switch
        {
            '"' => State.Second,
            _ => State.Accepted,
        };
    }

    /// <summary>
    /// Переход внутри имени или пути.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
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

    /// <summary>
    /// Переход после точки в пути.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
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

    /// <summary>
    /// Переход в целой части числа.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
    private State SwitchFromSixth(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => State.Sixth,
            '.' => State.Seventh,
            _ => State.Accepted,
        };
    }

    /// <summary>
    /// Переход после десятичной точки.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
    private State SwitchFromSeventh(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => State.Eighth,
            _ => State.TemplateError,
        };
    }

    /// <summary>
    /// Переход в дробной части числа.
    /// </summary>
    /// <param name="c">Очередной символ.</param>
    /// <returns>Новое состояние.</returns>
    private State SwitchFromEighth(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => State.Eighth,
            _ => State.Accepted,
        };
    }
    #endregion

    /// <summary>
    /// Можно ли закончить операнд в этом состоянии (конец строки допустим).
    /// </summary>
    /// <param name="state">Состояние автомата.</param>
    /// <returns>true для допускающих состояний.</returns>
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
