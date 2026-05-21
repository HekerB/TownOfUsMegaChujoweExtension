import os
import shutil

source_dir = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\TouMegaChujoweExtension"
target_dir = r"c:\Users\macie\Documents\GitHub\TownOfUsMegaChujoweExtension\ExportedRoles"
resources_dir = os.path.join(source_dir, "Resources")
target_resources_dir = os.path.join(target_dir, "Resources")

# Delete the exported .cs files from source
deleted_count = 0
for root, dirs, files in os.walk(target_dir):
    for file in files:
        if file.endswith(".cs"):
            target_path = os.path.join(root, file)
            rel_path = os.path.relpath(target_path, target_dir)
            source_path = os.path.join(source_dir, rel_path)
            
            if os.path.exists(source_path):
                os.remove(source_path)
                print(f"Deleted source file: {source_path}")
                deleted_count += 1

# Move icons
icons_to_move = [
    "Burst_Role_Icon.png",
    "Dad_Role_Icon.png",
    "Sentinel_Role_Icon.png",
    "Shockwave_Role_Icon.png",
    "Telecommunication_Role_Icon.png",
    "Zapper_Role_Icon.png"
]

os.makedirs(target_resources_dir, exist_ok=True)

for icon in icons_to_move:
    src_icon = os.path.join(resources_dir, icon)
    tgt_icon = os.path.join(target_resources_dir, icon)
    if os.path.exists(src_icon):
        shutil.move(src_icon, tgt_icon)
        print(f"Moved icon: {icon}")

# Also move button sprites if they exist
buttons_dir = os.path.join(resources_dir, "Buttons")
target_buttons_dir = os.path.join(target_resources_dir, "Buttons")
os.makedirs(target_buttons_dir, exist_ok=True)

if os.path.exists(buttons_dir):
    for button_file in os.listdir(buttons_dir):
        if "abyssbringer" in button_file.lower() or "burst" in button_file.lower() or "c4" in button_file.lower() or "dad" in button_file.lower() or "schrodingerscat" in button_file.lower() or "sentinel" in button_file.lower() or "shockwave" in button_file.lower() or "telecommunication" in button_file.lower() or "quarry" in button_file.lower() or "zapper" in button_file.lower():
            src_btn = os.path.join(buttons_dir, button_file)
            tgt_btn = os.path.join(target_buttons_dir, button_file)
            shutil.move(src_btn, tgt_btn)
            print(f"Moved button sprite: {button_file}")

# Move audio if they exist
audio_dir = os.path.join(resources_dir, "Audio")
target_audio_dir = os.path.join(target_resources_dir, "Audio")
os.makedirs(target_audio_dir, exist_ok=True)

if os.path.exists(audio_dir):
    for audio_file in os.listdir(audio_dir):
        if "abyssbringer" in audio_file.lower() or "burst" in audio_file.lower() or "c4" in audio_file.lower() or "dad" in audio_file.lower() or "schrodingerscat" in audio_file.lower() or "sentinel" in audio_file.lower() or "shockwave" in audio_file.lower() or "telecommunication" in audio_file.lower() or "quarry" in audio_file.lower() or "zapper" in audio_file.lower():
            src_aud = os.path.join(audio_dir, audio_file)
            tgt_aud = os.path.join(target_audio_dir, audio_file)
            shutil.move(src_aud, tgt_aud)
            print(f"Moved audio: {audio_file}")

print(f"Cleanup done! Deleted {deleted_count} source code files.")
