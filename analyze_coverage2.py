import json

# Parse coverage JSON
with open('tests/coverage.json', 'r') as f:
    coverage_data = json.load(f)

all_methods = []

# Navigate the structure: dll -> file -> namespace -> method_info
for dll_name, files in coverage_data.items():
    for file_path, namespaces in files.items():
        for ns_name, methods in namespaces.items():
            for method_name, method_info in methods.items():
                if isinstance(method_info, dict) and 'Lines' in method_info:
                    lines = method_info.get('Lines', {})
                    if lines:
                        covered = sum(1 for v in lines.values() if v > 0)
                        total = len(lines)
                        coverage_pct = (covered / total * 100) if total > 0 else 0
                        
                        file_key = file_path.split('\\')[-1]
                        # Extract just the method name from the full signature
                        method_short = method_name.split('::')[-1] if '::' in method_name else method_name
                        method_short = method_short.split('(')[0] if '(' in method_short else method_short
                        
                        all_methods.append({
                            'file': file_key,
                            'namespace': ns_name,
                            'method': method_short,
                            'full_method': method_name,
                            'covered': covered,
                            'total': total,
                            'coverage': coverage_pct,
                            'branches': method_info.get('Branches', [])
                        })

print(f"Total methods found: {len(all_methods)}\n")

# Group by file and calculate file coverage
from collections import defaultdict
coverage_by_file = defaultdict(lambda: {'lines_covered': 0, 'lines_total': 0, 'methods': []})

for method in all_methods:
    file_key = method['file']
    coverage_by_file[file_key]['lines_covered'] += method['covered']
    coverage_by_file[file_key]['lines_total'] += method['total']
    coverage_by_file[file_key]['methods'].append(method)

print("=== COVERAGE BY FILE (Sorted by Coverage) ===\n")
file_stats = []
for file_key in sorted(coverage_by_file.keys()):
    stats = coverage_by_file[file_key]
    file_coverage = (stats['lines_covered'] / stats['lines_total'] * 100) if stats['lines_total'] > 0 else 0
    file_stats.append((file_key, file_coverage, stats['lines_covered'], stats['lines_total']))

for file_key, cov, covered, total in sorted(file_stats, key=lambda x: x[1]):
    print(f"{file_key}: {cov:.1f}% ({covered}/{total})")

print("\n=== UNCOVERED METHODS (0% Coverage) ===\n")
uncovered = [m for m in all_methods if m['coverage'] == 0]
uncovered = sorted(uncovered, key=lambda x: -x['total'])

print(f"Total uncovered methods: {len(uncovered)}")
print(f"Total lines in uncovered methods: {sum(m['total'] for m in uncovered)}\n")

for i, method in enumerate(uncovered):
    if i >= 20:
        print(f"... and {len(uncovered) - 20} more")
        break
    print(f"{method['file']}::{method['namespace']}::{method['method']} - {method['total']} lines")

print("\n=== PARTIALLY COVERED METHODS (1-99%) - Top 15 ===\n")
partial = [m for m in all_methods if 0 < m['coverage'] < 100]
partial = sorted(partial, key=lambda x: x['coverage'])

for method in partial[:15]:
    uncovered_lines = method['total'] - method['covered']
    uncovered_branches = sum(1 for b in method['branches'] if b.get('Hits', 0) == 0)
    branch_str = f", {uncovered_branches} uncovered branches" if uncovered_branches > 0 else ""
    print(f"{method['file']}::{method['method']}: {method['coverage']:.0f}% ({method['covered']}/{method['total']}, {uncovered_lines} missing{branch_str})")

print("\n=== BRANCH COVERAGE ANALYSIS ===\n")
methods_with_uncovered_branches = [(m['file'], m['namespace'], m['method'], len([b for b in m['branches'] if b.get('Hits', 0) == 0]), len(m['branches'])) 
                                  for m in all_methods if any(b.get('Hits', 0) == 0 for b in m.get('branches', []))]
methods_with_uncovered_branches = sorted(methods_with_uncovered_branches, key=lambda x: -x[3])

print(f"Total methods with uncovered branches: {len(methods_with_uncovered_branches)}\n")
for file, ns, method, uncovered_cnt, total_cnt in methods_with_uncovered_branches[:15]:
    print(f"{file}::{method}: {uncovered_cnt}/{total_cnt} branches uncovered")

print("\n=== SUMMARY ===\n")
total_lines = sum(m['total'] for m in all_methods)
total_covered = sum(m['covered'] for m in all_methods)
print(f"Overall line coverage: {(total_covered / total_lines * 100):.1f}% ({total_covered}/{total_lines})")
print(f"Methods at 0% coverage: {len(uncovered)}")
print(f"Methods at 100% coverage: {len([m for m in all_methods if m['coverage'] == 100])}")
print(f"Methods with 1-99% coverage: {len(partial)}")
