import os

output_path = r'c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\VocabularyService\Services\A1A2LessonsSeeder.cs'

a1_topics = [
    ('Глагол to be: I am, he is, they are', 'A1', 1, 'Master the most fundamental verb in English.', 'R,W,S'),
    ('Указательные местоимения: this/that/these/those', 'A1', 2, 'Learn to point things out in English.', 'R,S'),
    ('Существительные: ед. и мн. число', 'A1', 3, 'Learn how to make nouns plural.', 'R,W'),
    ('Притяжательные местоимения: my/your/his', 'A1', 4, 'Talk about possession and ownership.', 'R,W,S'),
    ('Present Simple: утверждения', 'A1', 5, 'Talk about facts and daily routines.', 'W,S'),
    ('Present Simple: вопросы и отрицания', 'A1', 6, 'Learn to ask questions and say no.', 'W,S'),
    ('Артикли: a/an/the', 'A1', 7, 'Master the basics of English articles.', 'R,W'),
    ('There is / There are', 'A1', 8, 'Describe what exists around you.', 'R,S'),
    ('Глагол have/have got', 'A1', 9, 'Talk about what you possess.', 'W,S'),
    ('Предлоги места: in/on/at/next to', 'A1', 10, 'Describe where things are located.', 'R,S'),
    ('Прилагательные и порядок слов', 'A1', 11, 'Learn how to describe nouns with adjectives.', 'R,W'),
    ('Глагол can/can\'t: способность', 'A1', 12, 'Talk about what you can and cannot do.', 'W,S')
]

a2_topics = [
    ('Present Continuous', 'A2', 1, 'Talk about actions happening right now.', 'W,S'),
    ('Past Simple: правильные глаголы', 'A2', 3, 'Learn to talk about the past using regular verbs.', 'W,S'),
    ('Past Simple: неправильные глаголы', 'A2', 4, 'Master the common irregular verbs in the past.', 'W,S'),
    ('Past Simple: отрицание и вопрос', 'A2', 5, 'Ask questions and make negative sentences in the past.', 'W,S'),
    ('Past Continuous', 'A2', 6, 'Describe background actions in the past.', 'R,W'),
    ('Будущее: going to', 'A2', 7, 'Talk about your plans and intentions.', 'W,S'),
    ('Будущее: will (прогнозы, решения)', 'A2', 8, 'Make predictions and spontaneous decisions.', 'W,S'),
    ('Сравнительные степени', 'A2', 9, 'Compare two things.', 'R,W'),
    ('Превосходная степень', 'A2', 10, 'Describe the highest degree of a quality.', 'R,W'),
    ('Should/shouldn\'t: совет', 'A2', 11, 'Give and ask for advice.', 'W,S'),
    ('Исчисляемые/неисчисляемые и much/many', 'A2', 12, 'Talk about quantities.', 'R,W'),
    ('some/any/no + compounds', 'A2', 13, 'Use some, any, no and their compounds.', 'R,W'),
    ('Наречия частоты', 'A2', 14, 'Describe how often you do things.', 'W,S'),
    ('Вопросительные слова', 'A2', 15, 'Ask open-ended questions.', 'S,R')
]

content = '''using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public static class A1A2LessonsSeeder
{
    public static List<Lesson> GetLessons()
    {
        return new List<Lesson>
        {
'''

id_counter = 1

for t in a1_topics + a2_topics:
    level = t[1]
    color = 'from-emerald-500/20 to-emerald-600/10' if level == 'A1' else 'from-teal-500/20 to-teal-600/10'
    guid_str = f'22222222-0001-0000-0000-{id_counter:012d}'
    id_counter += 1
    
    content += f'''            new()
            {{
                Id = Guid.Parse("{guid_str}"),
                Title = "{t[0]}",
                Description = "{t[3]}",
                Category = "Grammar & Structure",
                Difficulty = "{'Starter' if level == 'A1' else 'Elementary'}",
                ColorCssClass = "{color}",
                CefrLevel = "{level}",
                OrderIndex = {t[2]},
                TargetSkills = "{t[4]}",
                EstimatedMinutes = 15,
                ContentMarkdown = """
## {t[0]}

### Rule
This is a basic rule for {t[0]}.

### Examples
- Example 1 for {t[0]}
- Example 2 for {t[0]}
""",
                SystemPrompt = "You are an English tutor helping the student practice {t[0]}. Start by explaining the concept briefly and giving 2 examples. Then ask 3 questions to check their understanding. Wait for their answers before moving to the next question."
            }},
'''

content += '''        };
    }
}
'''

with open(output_path, 'w', encoding='utf-8') as f:
    f.write(content)
