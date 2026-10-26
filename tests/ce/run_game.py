#!/usr/bin/env python3
"""Run CE integration checks in a disposable RimWorld 1.6 profile."""
import argparse
import os
import re
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STEAM = Path.home() / 'Library/Application Support/Steam/steamapps'


def xml(path, tag, values):
    root = ET.Element(tag)
    for k, v in values.items():
        n = ET.SubElement(root, k)
        if isinstance(v, list):
            for item in v:
                ET.SubElement(n, 'li').text = item
        else:
            n.text = str(v)
    ET.ElementTree(root).write(path, encoding='utf-8', xml_declaration=True)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--game', type=Path, default=STEAM / 'common/RimWorld')
    ap.add_argument('--workshop', type=Path, default=STEAM / 'workshop/content/294100')
    ap.add_argument('--framework', choices=['none', 'gear', 'apparel'], default='apparel')
    ap.add_argument('--without-built-in', action='store_true')
    ap.add_argument('--without-vef', action='store_true')
    args = ap.parse_args()
    game, workshop = args.game, args.workshop
    app = game / 'RimWorldMac.app'
    managed = app / 'Contents/Resources/Data/Managed'
    ce = workshop / '2890901044'
    version = (app / 'Version.txt').read_text().strip()
    if not version.startswith('1.6.'):
        raise RuntimeError('This harness requires RimWorld 1.6 and the installed 1.6 CE build')
    subprocess.run(['dotnet', 'build', str(ROOT / 'tests/ce/Runtime/Source/Runtime.csproj'),
                    f'-p:GameManagedPath={managed}', f'-p:CePath={ce}', '--nologo', '-v:q'], check=True)
    result = Path(tempfile.mkdtemp(prefix='demigryph-ce-'))
    profile = result / 'profile'
    (profile / 'Config').mkdir(parents=True)
    print('Results:', result, flush=True)
    if args.without_built_in:
        clone = result / 'ce'
        # Do not modify the Workshop copy. Symlink immutable content except the load map.
        clone.mkdir()
        for source in ce.iterdir():
            if source.name != 'LoadFolders.xml':
                (clone / source.name).symlink_to(source, target_is_directory=source.is_dir())
        tree = ET.parse(ce / 'LoadFolders.xml')
        for parent in tree.getroot():
            for entry in list(parent):
                if entry.text == 'ModPatches/pphhyy Demigryph':
                    parent.remove(entry)
        tree.write(clone / 'LoadFolders.xml', encoding='utf-8', xml_declaration=True)
        ce = clone
    sources = [('harmony', workshop / '2009463077'), ('ce', ce)]
    expansions = ['ludeon.rimworld.' + d for d in ['royalty', 'ideology', 'biotech', 'anomaly', 'odyssey']]
    mods = ['brrainz.harmony', 'ludeon.rimworld'] + expansions + ['ceteam.combatextended']
    if not args.without_vef:
        sources.append(('vef', workshop / '2023507013'))
        mods.append('oskarpotocki.vanillafactionsexpanded.core')
    if args.framework != 'none':
        id_, folder = ('dylan.animalgear', '1541438907') if args.framework == 'gear' else ('ingendum.animalapparelframework', '3513825850')
        mods.append(id_)
        sources.append(('framework', workshop / folder))
    sources.extend([('demigryphs', ROOT), ('tests', ROOT / 'tests/ce/Runtime')])
    mods.extend(['pphhyy.demigryphsmod', 'local.demigryphs.ce.tests'])
    xml(profile / 'Config/ModsConfig.xml', 'ModsConfigData', {'version': version.split()[0], 'activeMods': mods, 'knownExpansions': expansions})
    xml(profile / 'Config/Prefs.xml', 'PrefsData', {'screenWidth': 1280, 'screenHeight': 800, 'fullscreen': 'False', 'runInBackground': 'True', 'devMode': 'True', 'pauseOnError': 'False', 'volumeGame': 0, 'volumeMusic': 0})
    report = result / 'report.txt'
    links = []
    try:
        for name, source in sources:
            # Existing copies of CE are ignored when testing our private copy: pick by path below.
            link = app / 'Mods' / ('DEMI-CE-test-' + result.name + '-' + name)
            if any(p.resolve() == source.resolve() for p in (app / 'Mods').iterdir()):
                continue
            link.symlink_to(source, target_is_directory=True)
            links.append(link)
        env = dict(os.environ, DEMI_CE_REPORT=str(report))
        with (result / 'stdout.log').open('w') as log:
            process = subprocess.Popen([str(app / 'Contents/MacOS/RimWorld by Ludeon Studios'), f'-savedatafolder={profile}', '-quicktest', '-batchmode', '-logFile', str(result / 'Player.log')], cwd=game, env=env, stdout=log, stderr=subprocess.STDOUT)
            try:
                process.wait(timeout=300)
            except subprocess.TimeoutExpired:
                process.terminate()
                process.wait(timeout=15)
                raise RuntimeError('Game timed out; inspect ' + str(result))
        text = report.read_text() if report.exists() else ''
        print(text, flush=True)
        game_log = (result / 'Player.log').read_text(errors='replace')
        errors = re.findall(r'^.*(?:Exception|XML error|Could not resolve|Patch operation.*failed|Config error).*$' , game_log, re.MULTILINE)
        if errors:
            print('Game errors:', '\n'.join(errors), flush=True)
        if process.returncode != 0 or errors or 'FAIL ' in text or 'COMPLETE' not in text:
            raise RuntimeError('Game tests failed; inspect ' + str(result))
        print('PASS game run:', result)
    finally:
        for link in links:
            link.unlink()


if __name__ == '__main__':
    main()
