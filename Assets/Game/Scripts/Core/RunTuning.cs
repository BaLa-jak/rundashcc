namespace RunDashC
{
    /// <summary>
    /// Constantes de física compartidas por el gameplay y el diseño de niveles.
    /// Con gravedad 9.81*4: salto ≈ 2.6 tiles de alto, ~0.73 s en el aire (≈ 5.8 tiles a 8 u/s);
    /// doble salto ≈ 4.5 tiles de alto en total.
    /// </summary>
    public static class RunTuning
    {
        public const float RunSpeed = 8f;
        public const float GravityScale = 4f;
        public const float JumpVelocity = 14.3f;
        public const float DoubleJumpVelocity = 12.5f;
        /// <summary>Al soltar el botón subiendo, la velocidad vertical se multiplica por esto.</summary>
        public const float JumpCutMultiplier = 0.5f;
        public const float CoyoteTime = 0.1f;
        public const float JumpBufferTime = 0.12f;
        public const float InvulnerableTime = 1.5f;
        public const float StompBounceVelocity = 11f;
        public const int StompBits = 5;
        /// <summary>Si Camarada cae por debajo de esta Y, pierde una vida y reaparece en el checkpoint.</summary>
        public const float FallDeathY = -4f;

        // Reglas de diseño de nivel (en tiles).
        public const int MaxGapSingleJump = 4;
        public const int MaxGapDoubleJump = 7;
        public const int MaxStepUp = 2;
    }
}
