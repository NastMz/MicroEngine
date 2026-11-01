# Signing and secrets

Signing proves to a system that a package comes from you and has not changed. MicroEngine never signs for you and
never holds your keys: `microengine package` reads them from environment variables, passes passwords to the build
through the environment (never on a command line), masks them in its output, and never writes them into a package,
a template or a log. No MicroEngine account or service is involved.

## Android

Players' APKs are signed with **your own key**, created once and kept for the game's whole life: Android installs an
update only when it is signed with the same key, so losing it means players must uninstall before updating. The tool
never creates one.

```bash
keytool -genkeypair -keystore starcourier.keystore -alias starcourier -keyalg RSA -keysize 4096 -validity 10000 -dname "CN=Your Studio"
```

Keep the keystore and its passwords outside the repository (a password manager, your CI's secret store) and back it
up. To build:

| Variable | Value |
| --- | --- |
| `ANDROID_KEYSTORE` | path of the keystore file |
| `ANDROID_KEYSTORE_PASSWORD` | its password |
| `ANDROID_KEY_ALIAS` | the key's alias |
| `ANDROID_KEY_PASSWORD` | the key's password, when it differs from the store's |

Without `ANDROID_KEYSTORE` the build stops. `--debug-key` signs a test build with this machine's debug key instead;
such an APK installs on your devices but must never reach players (`build-info.json` says `"signing": "debug"`).

In CI, store the keystore as a base64 secret and decode it into a temporary file for the build, for example on
GitHub Actions:

```yaml
- name: Android package
  env:
    ANDROID_KEYSTORE_PASSWORD: ${{ secrets.ANDROID_KEYSTORE_PASSWORD }}
    ANDROID_KEY_ALIAS: ${{ secrets.ANDROID_KEY_ALIAS }}
  run: |
    echo "${{ secrets.ANDROID_KEYSTORE_BASE64 }}" | base64 -d > "$RUNNER_TEMP/release.keystore"
    ANDROID_KEYSTORE="$RUNNER_TEMP/release.keystore" microengine package android
```

## macOS

Apple silicon runs only signed code. Without anything set, the tool signs the app **ad hoc**: it runs, but macOS asks
the player to allow it once (Open Anyway in System Settings → Privacy & Security), because Apple has not notarized it.
To distribute without that step you need an Apple Developer Program membership:

| Variable | Value |
| --- | --- |
| `MICROENGINE_MACOS_SIGN_IDENTITY` | your "Developer ID Application: …" certificate's name, in the keychain; the app is signed with the hardened runtime and a secure timestamp |
| `MICROENGINE_MACOS_NOTARY_PROFILE` | a `notarytool` keychain profile (`xcrun notarytool store-credentials`); the tool then submits the app to Apple, waits, and staples the ticket |

Notarization needs a Developer ID signature; setting only the profile stops the build with that explanation.

## iOS

An iPhone or iPad installs only apps signed for it. `microengine package ios` needs:

| Variable | Value |
| --- | --- |
| `MICROENGINE_IOS_CODESIGN_KEY` | your "Apple Development: …" or "Apple Distribution: …" certificate's name |
| `MICROENGINE_IOS_PROVISIONING_PROFILE` | the name or UUID of a provisioning profile for the game's `ApplicationId` |

Both come from an Apple Developer account. `microengine package ios-simulator` needs neither, and its app runs only
in the Simulator.

## Windows

Packages are not code-signed by the tool; Windows SmartScreen may warn about an unknown publisher until a program has
a reputation. To sign, run `signtool` (Windows SDK) with your certificate on the executable before distributing; this
is the developer's step, not the tool's.

## In MicroEngine's own CI

The demos' release key lives in the repository's secrets (`ANDROID_KEYSTORE_BASE64` and the passwords). The SDK has
no stored nuget.org key: `sdk-release.yml` obtains a one-hour key through Trusted Publishing (GitHub OIDC, the
`NuGet/login` action), only in the `nuget` environment after its reviewers approve
([Publishing a release](sdk.md#publishing-a-release)).
Pull requests from forks never see either ([ADR 0014](../decisions/0014-public-repository-ci.md)).
