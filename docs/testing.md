# Credential extraction validation

The project uses a small, read-only `regf` parser rather than a third-party hive
package. It implements only navigation through `nk`/`lf`/`lh`/`li`/`ri` cells,
key class-name reads, and `vk` value reads. This keeps the distribution
self-contained and introduces no additional license obligations. In particular,
class names are supported because the SYSTEM boot key is stored in the class
metadata of the `JD`, `Skew1`, `GBG`, and `Data` keys, not in registry values.

## Disposable validation fixture

1. Create an isolated Windows Server VM, install AD DS, and promote it as the
   only controller of a throwaway forest. Never connect this forest to a
   production network.
2. Add a few test users with known, unique passwords. Change some passwords more
   than once (after setting a password-history policy), and populate accounts
   whose Kerberos AES keys and reversible-encryption data can be validated.
3. In an elevated prompt, create an IFM snapshot with `ntdsutil` (`activate
   instance ntds`, `ifm`, `create full <path>`). Copy the resulting `ntds.dit`
   and `SYSTEM` files to a local, access-controlled test directory.
4. Run ditjson against that pair, passing the database and matching hive as the
   two positional arguments (for example, `ditjson /fixture/ntds.dit
   /fixture/SYSTEM --output /fixture/domain.json`). Do **not** add the files or
   extracted output to Git; both contain reusable credential material.
5. In an isolated Python environment, install Impacket and run:

   ```text
   secretsdump.py -ntds /fixture/ntds.dit -system /fixture/SYSTEM -history LOCAL
   ```

   Compare ditjson's NT/LM hashes and history entries with the `.ntds` output,
   and its Kerberos and cleartext fields with `.ntds.kerberos` and
   `.ntds.cleartext`. Run Impacket with debug logging to compare its reported
   `Target system bootKey` value. A temporary local print of `NTDSHashes`' PEK
   list may be used to diagnose PEK mismatches, but must not be committed.

The deterministic unit tests remain safe for normal CI. A real fixture test must
be opt-in and run only where the untracked fixture directory is securely
available.
