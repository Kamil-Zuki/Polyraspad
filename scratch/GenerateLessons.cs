using System;
using System.IO;

class Program
{
    static void Main()
    {
        var outputPath = @"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\VocabularyService\Services\A1A2LessonsSeeder.cs";
        
        using (var writer = new StreamWriter(outputPath))
        {
            writer.WriteLine(@"using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public static class A1A2LessonsSeeder
{
    public static List<Lesson> GetLessons()
    {
        return new List<Lesson>
        {");

            // A1 Lessons
            var a1Topics = new[] {
                ("Глагол to be: I am, he is, they are", "A1", 1, "Master the most fundamental verb in English.", "R,W,S"),
                ("Указательные местоимения: this/that/these/those", "A1", 2, "Learn to point things out in English.", "R,S"),
                ("Существительные: ед. и мн. число", "A1", 3, "Learn how to make nouns plural.", "R,W"),
                ("Притяжательные местоимения: my/your/his", "A1", 4, "Talk about possession and ownership.", "R,W,S"),
                ("Present Simple: утверждения", "A1", 5, "Talk about facts and daily routines.", "W,S"),
                ("Present Simple: вопросы и отрицания", "A1", 6, "Learn to ask questions and say no.", "W,S"),
                ("Артикли: a/an/the", "A1", 7, "Master the basics of English articles.", "R,W"),
                ("There is / There are", "A1", 8, "Describe what exists around you.", "R,S"),
                ("Глагол have/have got", "A1", 9, "Talk about what you possess.", "W,S"),
                ("Предлоги места: in/on/at/next to", "A1", 10, "Describe where things are located.", "R,S"),
                ("Прилагательные и порядок слов", "A1", 11, "Learn how to describe nouns with adjectives.", "R,W"),
                ("Глагол can/can't: способность", "A1", 12, "Talk about what you can and cannot do.", "W,S")
            };

            int idCounter = 1;
            foreach (var t in a1Topics)
            {
                WriteLesson(writer, idCounter++, "A1", t.Item1, t.Item3, t.Item4, t.Item5, "Starter");
            }

            // A2 Lessons (Skipping 2 and 16 which are already in LessonSeeder, wait I'll just generate all new ones and we can remove the old ones or just add them all here and modify LessonSeeder to remove duplicates, actually let's just generate the missing ones.)
            var a2Topics = new[] {
                ("Present Continuous", "A2", 1, "Talk about actions happening right now.", "W,S"),
                ("Past Simple: правильные глаголы", "A2", 3, "Learn to talk about the past using regular verbs.", "W,S"),
                ("Past Simple: неправильные глаголы", "A2", 4, "Master the common irregular verbs in the past.", "W,S"),
                ("Past Simple: отрицание и вопрос", "A2", 5, "Ask questions and make negative sentences in the past.", "W,S"),
                ("Past Continuous", "A2", 6, "Describe background actions in the past.", "R,W"),
                ("Будущее: going to", "A2", 7, "Talk about your plans and intentions.", "W,S"),
                ("Будущее: will (прогнозы, решения)", "A2", 8, "Make predictions and spontaneous decisions.", "W,S"),
                ("Сравнительные степени", "A2", 9, "Compare two things.", "R,W"),
                ("Превосходная степень", "A2", 10, "Describe the highest degree of a quality.", "R,W"),
                ("Should/shouldn't: совет", "A2", 11, "Give and ask for advice.", "W,S"),
                ("Исчисляемые/неисчисляемые и much/many", "A2", 12, "Talk about quantities.", "R,W"),
                ("some/any/no + compounds", "A2", 13, "Use some, any, no and their compounds.", "R,W"),
                ("Наречия частоты", "A2", 14, "Describe how often you do things.", "W,S"),
                ("Вопросительные слова", "A2", 15, "Ask open-ended questions.", "S,R")
            };

            foreach (var t in a2Topics)
            {
                WriteLesson(writer, idCounter++, "A2", t.Item1, t.Item3, t.Item4, t.Item5, "Elementary");
            }

            writer.WriteLine(@"        };
    }
}");
        }
    }

    static void WriteLesson(StreamWriter writer, int id, string level, string title, int order, string desc, string skills, string diff)
    {
        string color = level == "A1" ? "from-emerald-500/20 to-emerald-600/10" : "from-teal-500/20 to-teal-600/10";
        string guidStr = $"22222222-{level == "A1" ? "0001" : "0002"}-0000-0000-{id:D12}";

        writer.WriteLine($@"            new()
            {{
                Id = Guid.Parse(""{guidStr}""),
                Title = ""{title}"",
                Description = ""{desc}"",
                Category = ""Grammar & Structure"",
                Difficulty = ""{diff}"",
                ColorCssClass = ""{color}"",
                CefrLevel = ""{level}"",
                OrderIndex = {order},
                TargetSkills = ""{skills}"",
                EstimatedMinutes = 15,
                ContentMarkdown = """"""
## {title}

### Rule
This is a basic rule for {title}.

### Examples
- Example 1 for {title}
- Example 2 for {title}
"""""",
                SystemPrompt = ""You are an English tutor helping the student practice {title}. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question.""
            }},");
    }
}
