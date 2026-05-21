import os
import shutil
import re

source_dir = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension"
target_dir = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\ExportedRoles"

roles_to_export = [
    "Abyssbringer", "Burst", "C4", "Dad", "SchrodingersCat", "Sentinel", 
    "Shockwave", "Telecommunication", "Quarry", "Zapper"
]

# Clean up target dir
if os.path.exists(target_dir):
    shutil.rmtree(target_dir)

os.makedirs(target_dir, exist_ok=True)

print("Searching for files related to roles...")

files_to_copy = set()

# Helper function to check if file relates to role
def matches_role(filepath, role):
    filename = os.path.basename(filepath)
    # Match the role name in filename
    if role.lower() in filename.lower():
        return True
    
    # Check contents for class definition
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            if f"class {role}Role" in content or f"class {role}Options" in content or f"class {role}Button" in content or f"class {role}Patches" in content or f"class {role}System" in content:
                return True
    except:
        pass
        
    return False

# Walk through the directory and find files
for root, dirs, files in os.walk(source_dir):
    for file in files:
        if not file.endswith('.cs'):
            continue
            
        filepath = os.path.join(root, file)
        
        for role in roles_to_export:
            if matches_role(filepath, role):
                files_to_copy.add(filepath)
                break

# Copy files preserving directory structure
for filepath in files_to_copy:
    rel_path = os.path.relpath(filepath, source_dir)
    target_path = os.path.join(target_dir, rel_path)
    
    os.makedirs(os.path.dirname(target_path), exist_ok=True)
    shutil.copy2(filepath, target_path)
    print(f"Copied {rel_path}")

print(f"Exported {len(files_to_copy)} files successfully.")
