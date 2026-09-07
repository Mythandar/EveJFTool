namespace EveJFTool.Core.Models;

public sealed record SolarSystem(
    int Id,
    string Name,
    double X,
    double Y,
    double Z,
    double SecurityStatus);
