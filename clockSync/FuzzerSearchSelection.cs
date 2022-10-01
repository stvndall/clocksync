using Spectre.Console;

namespace clockSync;

public class FuzzerSearchSelection<T>: IPrompt<T> 
{
    public T Show(IAnsiConsole console)
    {
        throw new NotImplementedException();
    }

    public Task<T> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}