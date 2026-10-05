using System.Collections.Generic;
using System.Linq;
using RedMoon.Core.Models;

namespace RedMoon.Core.Services
{
    /// <summary>
    /// Samler de daglige menstruationsregistreringer til hele menstruationer (start, slut, længde).
    ///
    /// Regler:
    /// - "Første dag" starter altid en ny menstruation.
    /// - "Efterfølgende dag" forlænger den igangværende menstruation (eller starter en ny, hvis der ikke er nogen).
    /// - "Sidste dag" afslutter menstruationen (slutdato bekræftet).
    /// - Har en menstruation varet over <see cref="MaxPeriodLengthDays"/> dage uden at blive afsluttet,
    ///   regnes en ny registrering som starten på en ny menstruation (brugeren har glemt at afslutte).
    /// - Er der mere end <see cref="MaxGapDays"/> dage siden sidste registrerede menstruationsdag,
    ///   regnes en ny registrering også som en ny menstruation (enkelte glemte dage midt i er tilladt).
    /// </summary>
    public static class PeriodBuilder
    {
        /// <summary>Længste menstruation vi antager, før vi starter en ny.</summary>
        public const int MaxPeriodLengthDays = 14;

        /// <summary>
        /// Største afstand (i dage) mellem to registrerede dage i samme menstruation.
        /// 3 betyder at op til to glemte dage i træk stadig hører til samme menstruation.
        /// </summary>
        public const int MaxGapDays = 3;

        public static List<MenstruationPeriod> Build(IEnumerable<DailyEntry> entries)
        {
            var result = new List<MenstruationPeriod>();
            LocalDate? start = null;
            var end = default(LocalDate);
            var confirmed = false;

            foreach (var entry in entries.Where(e => e.HasMenstruation).OrderBy(e => e.Date))
            {
                if (start.HasValue)
                {
                    var startsNew = entry.MenstruationStatus == MenstruationStatus.FirstDay
                                    || confirmed
                                    || entry.Date.DaysSince(start.Value) >= MaxPeriodLengthDays
                                    || entry.Date.DaysSince(end) > MaxGapDays;
                    if (startsNew)
                    {
                        result.Add(new MenstruationPeriod(start.Value, end, confirmed));
                        start = null;
                    }
                }

                if (!start.HasValue)
                {
                    start = entry.Date;
                    confirmed = false;
                }

                end = entry.Date;
                if (entry.MenstruationStatus == MenstruationStatus.LastDay)
                {
                    confirmed = true;
                }
            }

            if (start.HasValue)
            {
                result.Add(new MenstruationPeriod(start.Value, end, confirmed));
            }
            return result;
        }
    }
}
