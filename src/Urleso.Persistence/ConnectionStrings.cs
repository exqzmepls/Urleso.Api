﻿using System.ComponentModel.DataAnnotations;

namespace Urleso.Persistence;

internal sealed class ConnectionStrings
{
    public const string ConfigurationSection = "ConnectionStrings";

    [Required]
    public required string Postgresql { get; init; }
}