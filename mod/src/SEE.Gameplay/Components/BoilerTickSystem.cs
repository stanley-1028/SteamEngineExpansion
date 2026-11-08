using System;
using SEE.Core.Constants;
using SEE.Core.Logging;
using Timberborn.TickSystem;
using Timberborn.TimeSystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// The single 1 s tick entry point of the whole boiler family (requirements §13.3/§13.7,
    /// REQ-PERF-01/02). Instead of each entity ticking itself, this singleton accumulates game time
    /// and walks the registry once per configured interval, so the cost is
    /// <c>O(registered boilers)</c> per second and never <c>O(boilers)</c> per frame.
    ///
    /// It is a <see cref="ITickableSingleton"/>: the game periodically calls <see cref="Tick"/>, and
    /// Bindito registers implementations of the [Singleton]-marked interface automatically. The
    /// outermost try/catch keeps a broken registry or one broken boiler from stopping the game
    /// loop (REQ-SAFE-02).
    /// </summary>
    public sealed class BoilerTickSystem : ITickableSingleton
    {
        private readonly SEERuntime _runtime;
        private readonly SEEBoilerRegistry _registry;
        private readonly IDayNightCycle _dayNightCycle;
        private readonly ITickService _tickService;

        private double _pendingSeconds;
        private double _pendingGameHours;

        /// <summary>Creates the tick system with the game clock services.</summary>
        /// <param name="runtime">Gameplay runtime (log + config).</param>
        /// <param name="registry">Registry of active boilers.</param>
        /// <param name="dayNightCycle">Game clock; provides game hours per tick.</param>
        /// <param name="tickService">Game tick length in seconds.</param>
        public BoilerTickSystem(
            SEERuntime runtime,
            SEEBoilerRegistry registry,
            IDayNightCycle dayNightCycle,
            ITickService tickService)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _dayNightCycle = dayNightCycle ?? throw new ArgumentNullException(nameof(dayNightCycle));
            _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        }

        /// <inheritdoc />
        public void Tick()
        {
            try
            {
                Advance();
            }
            catch (Exception exception)
            {
                try
                {
                    _runtime.Log.Error(
                        SeeLogKeys.TickError,
                        "boiler tick system failed (" + exception.GetType().Name + ")");
                }
                catch (Exception)
                {
                    // Diagnostics must not escape into the game loop (REQ-SAFE-02).
                }
            }
        }

        private void Advance()
        {
            double tickSeconds = _tickService.TickIntervalInSeconds;
            if (double.IsNaN(tickSeconds) || double.IsInfinity(tickSeconds) || tickSeconds <= 0.0)
            {
                tickSeconds = SEEConst.TickIntervalSeconds;
            }

            double gameHours = _dayNightCycle.FixedDeltaTimeInHours;
            if (!double.IsNaN(gameHours) && !double.IsInfinity(gameHours) && gameHours > 0.0)
            {
                _pendingGameHours += gameHours;
            }

            _pendingSeconds += tickSeconds;
            double intervalSeconds = _runtime.Config.Perf.TickSeconds;
            if (double.IsNaN(intervalSeconds) || intervalSeconds <= 0.0)
            {
                intervalSeconds = SEEConst.TickIntervalSeconds;
            }

            if (_pendingSeconds < intervalSeconds)
            {
                return;
            }

            _pendingSeconds = 0.0;
            double hours = _pendingGameHours;
            _pendingGameHours = 0.0;
            if (hours <= 0.0 || _registry.RegisteredCount == 0)
            {
                return;
            }

            _registry.TickAll(hours);
        }
    }
}
