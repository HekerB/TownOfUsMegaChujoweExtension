import os
import xml.etree.ElementTree as ET

source_en = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension\Resources\Locale\en_US.xml"
source_pl = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension\Resources\Locale\pl_PL.xml"
target_dir = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\ExportedRoles"

roles = ["Abyssbringer", "Burst", "C4", "Dad", "SchrodingersCat", "Sentinel", "Shockwave", "Telecommunication", "Quarry", "Zapper"]

def extract_strings(filepath, output_filepath):
    if not os.path.exists(filepath):
        return
        
    tree = ET.parse(filepath)
    root = tree.getroot()
    
    with open(output_filepath, 'w', encoding='utf-8') as out:
        out.write("<!-- Poniżej wyeksportowane stringi do dodania do nowego pliku językowego -->\n")
        out.write("<Language>\n")
        
        for role in roles:
            out.write(f"  <!-- {role} -->\n")
            # Szukamy stringów, których name zaczyna się od ExtensionRole{Role} lub jakoś pasuje
            # Ponieważ nazwa roli w kluczach może mieć usunięte apostrofy itp (np SchrodingersCat)
            role_key = role.replace("'", "").replace(" ", "")
            
            for string_elem in root.findall('string'):
                name = string_elem.get('name', '')
                if role_key.lower() in name.lower() or name.startswith(f"ExtensionRole{role_key}"):
                    # Ręcznie re-konstruujemy element by zachować wcięcia i tekst
                    text = string_elem.text if string_elem.text else ""
                    out.write(f'  <string name="{name}">{text}</string>\n')
            out.write("\n")
            
        out.write("</Language>\n")

extract_strings(source_en, os.path.join(target_dir, "en_US_exported_strings.xml"))
extract_strings(source_pl, os.path.join(target_dir, "pl_PL_exported_strings.xml"))
print("Extracted strings to ExportedRoles")
