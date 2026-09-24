namespace Jev.Sdk;

internal static class EvaluationResponseValidator
{
    // Evita aceptar respuestas JSON que producirían decisiones incoherentes.
    public static void Validate(EvaluationResponse response, Dictionary<string, JevQuestion> questions)
    {
        // Probabilidades y confianza deben ser finitas y pertenecer al intervalo 0..1.
        static bool Unit(double value) => double.IsFinite(value) && value is >= 0 and <= 1;
        static bool Distribution(Dictionary<string, double>? values) => values is { Count: > 0 } && values.Values.All(Unit) && Math.Abs(values.Values.Sum() - 1) < 0.001;

        if (string.IsNullOrWhiteSpace(response.Model) || response.Answers is null || response.Usage is null || response.Usage.InputTokens < 0 || response.Usage.OutputTokens < 0)
            throw new JevResponseException("Incomplete evaluation response.");

        // Cada pregunta enviada debe tener una respuesta del tipo correspondiente.
        foreach (var (id, question) in questions)
        {
            if (!response.Answers.TryGetValue(id, out var answer)) throw new JevResponseException($"Missing answer '{id}'.");
            var valid = (question, answer) switch
            {
                (NoulQuestion, NoulAnswer noul) => Unit(noul.Noul),
                (ChoiceQuestion choice, ChoiceAnswer choiceAnswer) => choiceAnswer.Choice is not null && choice.Criteria.ContainsKey(choiceAnswer.Choice) && Unit(choiceAnswer.Confidence) && Distribution(choiceAnswer.Probabilities) && choice.Criteria.Keys.ToHashSet().SetEquals(choiceAnswer.Probabilities.Keys),
                (ScoreQuestion score, ScoreAnswer scoreAnswer) => double.IsFinite(scoreAnswer.Score) && scoreAnswer.Score >= 0 && scoreAnswer.Score <= score.Criteria.Count - 1 && Unit(scoreAnswer.Confidence) && Distribution(scoreAnswer.Probabilities) && scoreAnswer.Legend is not null && Enumerable.Range(0, score.Criteria.Count).Select(index => index.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToHashSet().SetEquals(scoreAnswer.Probabilities.Keys) && scoreAnswer.Legend.Keys.ToHashSet().SetEquals(scoreAnswer.Probabilities.Keys),
                _ => false
            };
            if (!valid) throw new JevResponseException($"Invalid answer '{id}'.");
        }
    }
}
