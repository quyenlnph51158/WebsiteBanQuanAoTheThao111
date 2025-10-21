﻿namespace API.DomainCusTomer.Config
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public int ExpireMinutes { get; set; }
    }
}
