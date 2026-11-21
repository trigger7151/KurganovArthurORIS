namespace CardsApp.Services.Validation;

public record SaveResult(ValidationErrors Errors, int Id)
{
    public bool Success => Errors.IsValid;
}
