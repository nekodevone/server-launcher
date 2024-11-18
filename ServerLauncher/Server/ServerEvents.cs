namespace ServerLauncher.Server
{
    public static class ServerEvents
    {
        public static Event Tick { get; set; } = new();
    
        public static Event Crashed { get; set; } = new();
    
        public static Event Full { get; set; } = new();
    
        public static Event IdleEntered { get; set; } = new();
    
        public static Event IdleExited { get; set; } = new();
    
        public static Event RoundEnded { get; set; } = new();
    
        public static Event RoundStarted { get; set; } = new();
    
        public static Event Started { get; set; } = new();
    
        public static Event Starting { get; set; } = new();
    
        public static Event Stopped { get; set; } = new();
    
        public static Event WaitingForPlayers { get; set; } = new();

        public static void OnTick() => Tick.InvokeSafely();
    
        public static void OnCrashed() => Crashed.InvokeSafely();
    
        public static void OnFull() => Full.InvokeSafely();
    
        public static void OnIdleEntered() => IdleEntered.InvokeSafely();
    
        public static void OnIdleExited() => IdleExited.InvokeSafely();
    
        public static void OnRoundEnded() => RoundEnded.InvokeSafely();
    
        public static void OnRoundStarted() => RoundStarted.InvokeSafely();
    
        public static void OnStarted() => Started.InvokeSafely();
    
        public static void OnStarting() => Starting.InvokeSafely();
    
        public static void OnStopped() => Stopped.InvokeSafely();
    
        public static void OnWaitingForPlayers() => WaitingForPlayers.InvokeSafely();
    }
}