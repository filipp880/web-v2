namespace TramWebApp.Application.Validators;

using FluentValidation;
using TramWebApp.Application.DTOs;

public class CreateTramRunDtoValidator : AbstractValidator<CreateTramRunDto>
{
    public CreateTramRunDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название обязательно")
            .MaximumLength(100).WithMessage("Название не может превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Описание не может превышать 500 символов");
    }
}

public class UpdateTramRunDtoValidator : AbstractValidator<UpdateTramRunDto>
{
    public UpdateTramRunDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название обязательно")
            .MaximumLength(100).WithMessage("Название не может превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Описание не может превышать 500 символов");
    }
}

public class CreateTelemetryPointDtoValidator : AbstractValidator<CreateTelemetryPointDto>
{
    public CreateTelemetryPointDtoValidator()
    {
        RuleFor(x => x.SequenceNumber)
            .GreaterThanOrEqualTo(0).WithMessage("Номер последовательности должен быть неотрицательным");

        RuleFor(x => x.Timestamp)
            .GreaterThanOrEqualTo(0).WithMessage("Временная метка должна быть неотрицательной");

        RuleFor(x => x.ControllerHandlePosition)
            .InclusiveBetween(-1, 1).WithMessage("Положение ручки должно быть в диапазоне [-1, 1]");

        RuleFor(x => x.FrontBogieSpeed)
            .GreaterThanOrEqualTo(0).WithMessage("Скорость передней тележки должна быть неотрицательной");

        RuleFor(x => x.RearBogieSpeed)
            .GreaterThanOrEqualTo(0).WithMessage("Скорость задней тележки должна быть неотрицательной");
    }
}

public class ModelInputDtoValidator : AbstractValidator<ModelInputDto>
{
    public ModelInputDtoValidator()
    {
        RuleFor(x => x.ControllerHandlePosition)
            .InclusiveBetween(-1, 1).WithMessage("Положение ручки должно быть в диапазоне [-1, 1]");

        RuleFor(x => x.FrontBogieSpeed)
            .GreaterThanOrEqualTo(0).WithMessage("Скорость передней тележки должна быть неотрицательной");

        RuleFor(x => x.RearBogieSpeed)
            .GreaterThanOrEqualTo(0).WithMessage("Скорость задней тележки должна быть неотрицательной");

        RuleFor(x => x.Timestamp)
            .GreaterThanOrEqualTo(0).WithMessage("Временная метка должна быть неотрицательной");
    }
}

public class InferenceRequestDtoValidator : AbstractValidator<InferenceRequestDto>
{
    public InferenceRequestDtoValidator()
    {
        RuleFor(x => x.TramRunId)
            .NotEqual(Guid.Empty).WithMessage("ID прогона обязателен");

        RuleFor(x => x.SequenceNumber)
            .GreaterThanOrEqualTo(0).WithMessage("Номер последовательности должен быть неотрицательным");

        RuleFor(x => x.Input)
            .NotNull().WithMessage("Входные данные обязательны")
            .SetValidator(new ModelInputDtoValidator());
    }
}

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MinimumLength(3).WithMessage("Имя пользователя должно содержать минимум 3 символа")
            .MaximumLength(50).WithMessage("Имя пользователя не может превышать 50 символов")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Имя пользователя может содержать только буквы, цифры и подчеркивание");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(100).WithMessage("Email не может превышать 100 символов");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов")
            .MaximumLength(100).WithMessage("Пароль не может превышать 100 символов");
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен");
    }
}