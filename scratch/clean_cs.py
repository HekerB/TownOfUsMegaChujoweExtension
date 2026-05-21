import os
import re

icons_file = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension\Assets\TouExtensionIcons.cs"
rpc_file = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension\Networking\ExtensionRpc.cs"
target_dir = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\ExportedRoles"

roles = ["Abyssbringer", "Burst", "C4", "Dad", "SchrodingersCat", "Sentinel", "Shockwave", "Telecommunication", "Quarry", "Zapper"]

# Helper to check if a line matches any role
def matches_role(line, role_names):
    for role in role_names:
        # Check specific keywords
        if role.lower() in line.lower():
            return True
        if role == "SchrodingersCat" and "Cat" in line and not "Charlatan" in line:
            return True
    return False

def clean_file(filepath, out_filename, is_rpc=False):
    if not os.path.exists(filepath):
        return
        
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    new_lines = []
    removed_lines = []
    
    for line in lines:
        match = False
        
        # specific matches for RPC to avoid removing unrelated 'Cat' or 'Burst' 
        for role in roles:
            role_key = role.replace("'", "").replace(" ", "")
            if is_rpc:
                # RPC enums format: RoleNameAction = 123,
                if re.search(r'\b' + role_key + r'[A-Z]', line) or re.search(r'\bCat[A-Z]', line) if role == "SchrodingersCat" else False:
                    match = True
                    break
            else:
                # Icons format: public static LoadableAsset<Sprite> RoleNameRoleIcon
                if role_key + "RoleIcon" in line or role_key + "Role" in line:
                    match = True
                    break
                    
        if match:
            removed_lines.append(line.strip())
        else:
            new_lines.append(line)
            
    with open(filepath, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
        
    with open(os.path.join(target_dir, out_filename), 'w', encoding='utf-8') as f:
        f.write("\n".join(removed_lines))

clean_file(icons_file, "ExportedIcons.txt", is_rpc=False)
clean_file(rpc_file, "ExportedRPCs.txt", is_rpc=True)
print("Finished cleaning TouExtensionIcons and ExtensionRpc")
