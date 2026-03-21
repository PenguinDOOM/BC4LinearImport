# BC4LinearImport

**言語:** [English](README.md) | 日本語

BC4LinearImport は、Unity が BC4 向けとして扱うグレースケール `PNG` / `JPG` / `JPEG` テクスチャを、sRGB ではなく linear data として読み込めるようにするためのエディター拡張です。

VRChat のアバターやワールドでグレースケールテクスチャを使うときに、毎回インポート設定を手で見直す手間を減らしたい場合に役立ちます。

## このツールでできること

- 条件に合うテクスチャを、インポート時に自動で処理できます。
- 触ってほしくないアセットやフォルダーを除外できます。
- 設定を変えたあと、すでに入っているテクスチャをまとめて再処理できます。
- 「なぜ対象になったのか / ならなかったのか」を確認したいときに、診断メニューを使えます。

## ざっくり使い方

1. `Project Settings > BC4 Linear Import` を開きます。
2. プロジェクト全体で機能を使いたい場合は、`Enable BC4 linear import` を有効のままにします。
3. 一部のテクスチャやフォルダーだけ除外したい場合は、`Excluded assets and folders` に追加します。
4. 設定を変える前からプロジェクト内にあったテクスチャは、再インポートを実行してもう一度処理させます。
5. 対象判定の理由がわかりにくいときは、診断メニューで挙動を確認します。

## どこにある？

- `Project Settings > BC4 Linear Import`
  - `Enable BC4 linear import`、`Excluded assets and folders`、`Reimport Eligible PNG/JPG/JPEG Textures` をまとめて確認するメイン画面です。
- `Tools > BC4 Linear Import > Reimport Eligible Textures`
  - 現在の条件に合うテクスチャを、プロジェクト全体からまとめて再インポートします。
- `Tools > BC4 Linear Import > Diagnostics > Observe Targeting`
  - テスト用の観測ログを出して、Unity がそのテクスチャをどう扱っているかを確認するときに使います。

## このドキュメントの範囲

このドキュメントは、BC4LinearImport がすでに Unity プロジェクトへ入っている前提で書かれています。

そのあとの使い方を説明するもので、インストール手順や導入方法は扱いません。

## 次に読むページ

- [使い方ガイド](docs/usage.ja.md)
- [Troubleshooting guide (English)](docs/troubleshooting.md)

もし `0 eligible textures` と表示されたり、テクスチャが変わらなかったりした場合は、まず [Troubleshooting guide (English)](docs/troubleshooting.md) を見るのがおすすめです。