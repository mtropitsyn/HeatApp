using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var calculations = await _db.Calculations
            .OrderByDescending(c => c.CreatedAt)
            .Take(30)
            .ToListAsync();

        ViewBag.SavedCalculations = calculations;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Calculate([FromBody] CalculationInput input)
    {
        if (input == null) return Json(new { success = false, error = "Данные не получены" });

        input.Material ??= new Material();
        input.Gas ??= new Gas();
        input.Parameters ??= new Parameters();

        if (input.Parameters.Height <= 0 || input.Parameters.CrossSection <= 0 ||
            input.Parameters.MaterialFlowRate <= 0 || input.Parameters.GasFlowRate <= 0 ||
            input.Parameters.VolumetricHeatTransferCoeff <= 0)
        {
            return Json(new { success = false, error = "Проверьте значения: высота, площадь, расходы и α_v должны быть > 0" });
        }

        try
        {
            var result = PerformCalculation(input);

            var calculation = new Calculation
            {
                Name = string.IsNullOrWhiteSpace(input.Name) ? "Расчёт " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") : input.Name.Trim(),
                Description = input.Description,
                InputJson = JsonSerializer.Serialize(input),
                ResultJson = JsonSerializer.Serialize(result)
            };

            _db.Calculations.Add(calculation);
            await _db.SaveChangesAsync();

            return Json(new { success = true, result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Ошибка: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCalculation(int id)
    {
        var calc = await _db.Calculations.FindAsync(id);
        if (calc == null)
            return NotFound();

        var input = JsonSerializer.Deserialize<CalculationInput>(calc.InputJson)!;
        var result = JsonSerializer.Deserialize<CalculationResult>(calc.ResultJson)!;

        return Json(new
        {
            id = calc.Id,
            name = calc.Name,
            description = calc.Description,
            material = input.Material,
            gas = input.Gas,
            parameters = input.Parameters,
            result
        });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCalculation(int id)
    {
        var calc = await _db.Calculations.FindAsync(id);
        if (calc == null)
            return Json(new { success = false, error = "Расчёт не найден" });

        _db.Calculations.Remove(calc);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    public async Task<IActionResult> ExportToCsv(int id)
    {
        var calc = await _db.Calculations.FindAsync(id);
        if (calc == null)
            return NotFound();

        var result = JsonSerializer.Deserialize<CalculationResult>(calc.ResultJson)!;

        var sb = new StringBuilder();
        sb.AppendLine("Высота (м);T материала (°C);T газа (°C);ΔT (°C)");

        for (int i = 0; i < result.Heights.Count; i++)
        {
            sb.AppendLine($"{result.Heights[i]:F3};{result.MaterialTemperatures[i]:F1};{result.GasTemperatures[i]:F1};{result.TemperatureDifferences[i]:F1}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"{calc.Name.Replace(" ", "_")}.csv");
    }

    private CalculationResult PerformCalculation(CalculationInput input)
    {
        var p = input.Parameters;

        // Теплоёмкость потока материала, Вт/(м²·°C)
        double Cm = (p.MaterialFlowRate / 3600) * p.MaterialSpecificHeat / p.CrossSection;

        // Теплоёмкость потока газа
        double Cg;
        if (p.IsGasHeatCapacityVolumetric)
        {
            // C_g объёмная, кДж/(м³·°C) → переводим в Дж/(м³·°C), затем в Вт/(м²·°C)
            double Cg_vol = p.GasSpecificHeat * 1000; // кДж → Дж
            double volumetricFlow = p.GasFlowRate / 3600; // м³/ч → м³/с
            Cg = Cg_vol * (volumetricFlow / p.CrossSection);
        }
        else
        {
            Cg = (p.GasFlowRate / 3600) * p.GasSpecificHeat / p.CrossSection;
        }

        double alphaV = p.VolumetricHeatTransferCoeff;
        double m = Cm / Cg;

        // Аналитическое решение из документа
        var heights = new List<double>();
        var tMat = new List<double>();
        var tGas = new List<double>();
        var deltaT = new List<double>();

        int steps = p.CalculationSteps;
        for (int i = 0; i <= steps; i++)
        {
            double Y = i * (1.0 / steps); // от 0 (низ) до 1 (верх)
            double y = Y * p.Height;

            double theta_mat, theta_gas;

            if (Math.Abs(m - 1) < 1e-6)
            {
                double exp_term = Math.Exp(-alphaV * p.Height * (1 - Y) / Cm);
                theta_mat = p.GasInletTemp + (p.MaterialInletTemp - p.GasInletTemp) * Y * (1 - exp_term);
                theta_gas = theta_mat - (p.MaterialInletTemp - p.GasInletTemp) * (1 - exp_term);
            }
            else
            {
                double exp1 = Math.Exp(-(1 - m) * alphaV * p.Height * Y / Cm);
                double exp2 = Math.Exp(-(1 - m) * alphaV * p.Height / Cm);
                double denom = 1 - m * exp2;

                double A = (p.MaterialInletTemp - p.GasInletTemp) * (1 - exp1) / denom;

                theta_mat = p.GasInletTemp + A;
                theta_gas = p.GasInletTemp + m * A;
            }

            tMat.Add(theta_mat);
            tGas.Add(theta_gas);
            deltaT.Add(Math.Abs(theta_mat - theta_gas));
            heights.Add(y);
        }

        double Q = Math.Abs(Cm * p.CrossSection * (tMat[0] - p.MaterialInletTemp)) / 1000;
        double Cmin = Math.Min(Cm, Cg) * p.CrossSection;
        double eff = Cmin > 0 ? Q * 1000 / (Cmin * Math.Abs(p.MaterialInletTemp - p.GasInletTemp)) * 100 : 0;

        return new CalculationResult
        {
            VolumetricHeatTransferCoefficient = alphaV,
            TotalHeatTransfer = Q,
            Efficiency = eff,
            MaterialOutletTemp = tMat[0],
            GasOutletTemp = tGas[^1],
            Heights = heights,
            MaterialTemperatures = tMat,
            GasTemperatures = tGas,
            TemperatureDifferences = deltaT
        };
    }
}