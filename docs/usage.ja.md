# BC4LinearImport 使い方ガイド

**言語:** [English](usage.md) | 日本語

このガイドでは、BC4LinearImport を Unity プロジェクトに導入したあと、普段どう使えばいいかを説明します。

短く言うと、`Project Settings > BC4 Linear Import` を開き、`Enable BC4 linear import` を有効のままにして、スキップしたいテクスチャやフォルダーだけを除外し、設定を変える前からプロジェクト内にあったテクスチャには再インポートを実行します。

## 概要

BC4LinearImport は、条件に合うグレースケールの `PNG` / `JPG` / `JPEG` テクスチャを、Unity が sRGB ではなく linear data として取り込めるようにするためのツールです。

ここで大事なのは `eligible` という考え方です。このツールは、Unity が BC4 向けとして扱っているテクスチャを対象にしています。

普段使いでは、次の条件にしておくのがいちばん安全でわかりやすいです。

- テクスチャがグレースケールの `PNG`、`JPG`、または `JPEG` である
- Unity 側で、そのテクスチャが明示的に `Standalone BC4` を使う設定になっている

また、`Single Channel` のテクスチャに対して Unity が自動的に BC4 扱いするパターンもありますが、こちらは条件次第です。普段使いでは「上級者向けのケース」であり、常にそうなる保証ではないと考えるのが安全です。

## 始める前に

設定を触る前に、次の実用的なポイントを押さえておくと安心です。

- このガイドはインストールや導入手順を扱いません。BC4LinearImport がすでにプロジェクトへ入っている前提です。
- 使いやすい対応ファイル形式は `PNG`、`JPG`、`JPEG` です。
- いちばん安全な対象条件は、明示的に `Standalone BC4` を使うことです。
- テクスチャが明示的に `Standalone BC4` でない場合は、対象になると決め打ちしないでください。
- `Single Channel` の自動 BC4 挙動に頼る場合は、条件付きだと考えて必要に応じて確認してください。

## 設定ページを開く

1. Unity を開きます。
2. `Project Settings > BC4 Linear Import` を開きます。
3. 主に次の項目が表示されます。
   - `Enable BC4 linear import`
   - `Excluded assets and folders`
   - `Reimport Eligible PNG/JPG/JPEG Textures`

普段いちばんよく使うのは、この設定ページです。

## 機能を有効 / 無効にする

`Enable BC4 linear import` は、プロジェクト全体でこの機能を使うかどうかを切り替える項目です。

- **有効:** 条件に合う対応テクスチャは、インポート時に処理されるようになります。
- **無効:** このプロジェクトでは BC4LinearImport がテクスチャに対して動作しなくなります。

数枚だけスキップしたい場合は、機能そのものを切るより、通常は有効のままにして除外設定を使うほうが扱いやすいです。

## どのテクスチャがいちばん安全に使えるかを知る

Unity の内部仕様を細かく覚える必要はありませんが、この区別だけは大切です。

- **おすすめ / いちばん安全:** テクスチャが明示的に `Standalone BC4` に設定されている
- **上級者向け / 条件付き:** Unity が `Single Channel` テクスチャを Standalone で自動的に BC4 扱いする

いちばん予測しやすい結果がほしいなら、明示的に `Standalone BC4` を使うルートがおすすめです。

言い換えると、BC4LinearImport は「グレースケール画像なら何でも変換するボタン」ではありません。Unity がすでに BC4 向けとして扱っているテクスチャだけが対象です。

## 特定のアセットやフォルダーを除外する

特定のテクスチャを BC4LinearImport の対象外にしたいときは、除外設定を使います。

### 除外を追加する

1. `Project Settings > BC4 Linear Import` を開きます。
2. `Excluded assets and folders` を探します。
3. `Drop project assets or folders here` と表示されたエリアに、プロジェクト内のアセットまたはフォルダーをドラッグ＆ドロップします。
4. 追加した項目が `Stored exclusion paths` に表示されることを確認します。

パスを手入力する必要はありません。想定されている使い方は、プロジェクトからのドラッグ＆ドロップです。

### 除外を 1 件だけ外す

- `Stored exclusion paths` で、外したい項目の横にある `Remove` をクリックします。

### すべて消す

- `Clear all` をクリックすると、保存されている除外をすべて削除できます。

### 除外が影響する範囲

除外は次のすべてに影響します。

- 除外したそのアセット自身
- 除外したフォルダーの中にあるすべてのもの
- 通常のインポート時の自動処理
- 手動の一括再インポート

つまり、あるテクスチャを除外すると、このあと説明する再インポート機能でもスキップされます。

## すでにプロジェクト内にあるテクスチャを再インポートする

BC4LinearImport は、条件に合うテクスチャがインポートされるときに自動で動作します。そのため、今の設定に変える前からプロジェクトに入っていたテクスチャは、手動で再処理が必要になることがあります。

次のようなときは、一括再インポートを実行してください。

- `Enable BC4 linear import` を有効にした直後
- 除外を外して、以前スキップされていたテクスチャをもう一度処理したいとき
- テクスチャの設定を変えて BC4 向けになったので、インポート処理をやり直したいとき
- 昔にインポートしたテクスチャが、今の設定で処理されていたか自信がないとき

### `Project Settings` から再インポートする

使うボタン:

- `Reimport Eligible PNG/JPG/JPEG Textures`

場所:

- `Project Settings > BC4 Linear Import`

### `Tools` メニューから再インポートする

使うメニュー:

- `Tools > BC4 Linear Import > Reimport Eligible Textures`

### 再インポートで実際に行われること

再インポートを実行すると、プロジェクト内を走査して、次の条件をすべて満たすテクスチャだけを再インポートします。

- `PNG`、`JPG`、または `JPEG`
- 除外されていない
- `Enable BC4 linear import` によって許可されている
- Unity が現在 BC4 向けとして扱っている

思ったより再インポート数が少ない場合、よくある理由は次のとおりです。

- テクスチャが除外されている
- プロジェクト全体で機能が無効になっている
- そのテクスチャが現在 BC4 向けとして扱われていない
- ファイルが `PNG`、`JPG`、`JPEG` ではない

「なぜ変換されなかったの？」をもう少し詳しく確認したい場合は、[Troubleshooting guide (English)](troubleshooting.md) を見てください。

## 挙動がわかりにくいときは診断を使う

期待した動きにならなかったときに、原因が **BC4 向け判定** 側なのかよくわからない場合は、次を使います。

- `Tools > BC4 Linear Import > Diagnostics > Observe Targeting`

特に役立つのは、次のような場面です。

- Unity の環境で、`Single Channel` の自動 BC4 パスが見えているか試したいとき
- 「このテクスチャは BC4 向けのはず」と思い込む前に、もう少し根拠を確認したいとき
- Unity が Standalone 側で BC4 をどう報告しているかを、このツールが期待する形で確認したいとき

これは修正ボタンではなく、観測用の補助ツールだと考えてください。通常 UI だけでは判断しにくいときに、「この設定はここで BC4 向けとして見えているのか？」を確かめる助けになります。

## 普段の使い方のまとめ

多くのユーザーにとって、いちばん簡単な流れは次のとおりです。

1. `Project Settings > BC4 Linear Import` を開きます。
2. `Enable BC4 linear import` を有効のままにします。
3. スキップしたいテクスチャやフォルダーだけを除外します。
4. いちばん安全な結果がほしいときは、明示的な `Standalone BC4` 設定を優先します。
5. 今の設定に変える前からテクスチャが入っていた場合は、`Reimport Eligible PNG/JPG/JPEG Textures` または `Tools > BC4 Linear Import > Reimport Eligible Textures` を実行します。
6. それでも挙動がわかりにくいときは、`Tools > BC4 Linear Import > Diagnostics > Observe Targeting` を試します。

## 関連ドキュメント

- [README（日本語）](../README.ja.md)
- [Usage guide (English)](usage.md)
- [Troubleshooting guide (English)](troubleshooting.md)