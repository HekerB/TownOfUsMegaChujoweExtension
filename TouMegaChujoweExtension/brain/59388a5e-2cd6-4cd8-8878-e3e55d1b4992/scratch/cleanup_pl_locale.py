import os

file_path = r'c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension\Resources\Locale\pl_PL.xml'

replacements = {
    'ą': 'a', 'ć': 'c', 'ę': 'e', 'ł': 'l', 'ń': 'n', 'ó': 'o', 'ś': 's', 'ź': 'z', 'ż': 'z',
    'Ą': 'A', 'Ć': 'C', 'Ę': 'E', 'Ł': 'L', 'Ń': 'N', 'Ó': 'O', 'Ś': 'S', 'Ź': 'Z', 'Ż': 'Z'
}

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

for original, replacement in replacements.items():
    content = content.replace(original, replacement)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Character cleanup complete.")
