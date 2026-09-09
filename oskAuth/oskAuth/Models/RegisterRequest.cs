using JetBrains.Annotations;

using System.Text;

namespace oskAuth.Models;

[UsedImplicitly]
internal sealed record RegisterRequest(string? Login, string? Password, string? DisplayName)
{
    public string Validate()
    {
        var errors = new StringBuilder();

        if (string.IsNullOrWhiteSpace(Login))
        {
            errors.AppendLine("Куда без логина?");
        }
        else if (Login.Trim().Length > 100)
        {
            errors.AppendLine("Логин больше 100 символов? Ты действуешь наверняка");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            errors.AppendLine("Введите имя");
        }
        else if (DisplayName.Trim().Length > 100)
        {
            errors.AppendLine("В паспорте тоже больше 100 букв? если да - напиши в тех поддержку");
        }

        if (string.IsNullOrEmpty(Password))
        {
            errors.AppendLine("Пустой пароль? Серьезно?");
        }
        else switch (Password.Length)
        {
            case < 8:
                errors.AppendLine("Пароль должен быть не меньше 8 символов");
                break;
            case > 128:
                errors.AppendLine("Пароль должен быть не больше 128 символов. Тяжело будет потом с другого устройства зайти");
                break;
        }

        return errors.ToString().TrimEnd();
    }
}