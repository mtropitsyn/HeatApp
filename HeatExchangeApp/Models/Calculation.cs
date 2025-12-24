using System.ComponentModel.DataAnnotations;

public class Calculation
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "Без названия";

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Входные данные — сохраняем как JSON-строку
    public string InputJson { get; set; } = string.Empty;

    // Результаты расчёта — тоже как JSON-строка
    public string ResultJson { get; set; } = string.Empty;
}

// Классы для десериализации входных данных (точно совпадают с тем, что отправляет JavaScript)
public class CalculationInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    public Material Material { get; set; } = new();
    public Gas Gas { get; set; } = new();
    public Parameters Parameters { get; set; } = new();
}

public class Material
{
    public string? Name { get; set; }
    public double Density { get; set; }
    public double SpecificHeat { get; set; }
    public double ParticleSize { get; set; }
    public double Porosity { get; set; }
}

public class Gas
{
    public string? Name { get; set; }
    public double Density { get; set; }
    public double SpecificHeat { get; set; }
    public double Viscosity { get; set; }
    public double ThermalConductivity { get; set; }
}

public class Parameters
{
    public double Height { get; set; } // H0, м — высота слоя
    public double CrossSection { get; set; } // S, м² — площадь сечения аппарата
    public double MaterialFlowRate { get; set; } // G_m, кг/ч — массовый расход материала
    public double GasFlowRate { get; set; } // V_g, кг/ч — массовый расход газа (или объёмный, если C_g объёмная)
    public double MaterialInletTemp { get; set; } // t', °C — температура материала на входе (сверху)
    public double GasInletTemp { get; set; } // T', °C — температура газа на входе (снизу)
    public double MaterialSpecificHeat { get; set; } // C_m, Дж/(кг·°C) — удельная теплоёмкость материала
    public double GasSpecificHeat { get; set; } // C_g — может быть Дж/(кг·°C) или кДж/(м³·°C) — смотри пояснение ниже
    public bool IsGasHeatCapacityVolumetric { get; set; } = true; // true — если C_g объёмная (кДж/(м³·°C)), false — массовая
    public double VolumetricHeatTransferCoeff { get; set; } // α_v, Вт/(м³·°C) — объёмный коэффициент теплоотдачи
    public int CalculationSteps { get; set; } = 400;
}

// Класс для результата расчёта (то, что возвращаем клиенту и сохраняем)
public class CalculationResult
{
    public double HeatTransferCoefficient { get; set; } // α, Вт/(м²·°C)
    public double VolumetricHeatTransferCoefficient { get; set; } // h_v, Вт/(м³·°C)
    public double TotalHeatTransfer { get; set; } // кВт
    public double Efficiency { get; set; } // %

    public double MaterialOutletTemp { get; set; }
    public double GasOutletTemp { get; set; }

    public List<double> Heights { get; set; } = new();
    public List<double> MaterialTemperatures { get; set; } = new();
    public List<double> GasTemperatures { get; set; } = new();
    public List<double> TemperatureDifferences { get; set; } = new();
}