## 概要

本プロジェクトは、C# で実装した HTTP/HTTPS 通信の動作検証を目的としたWPF デスクトップアプリケーションのポートフォリオです。

クライアントアプリケーション（HttpClientModule）とサーバーアプリケーション（HttpServerModule）で構成されており、本リポジトリは **サーバーアプリケーション** に該当します。

サーバー情報をクライアント／サーバー両アプリケーションの設定として登録した後、
クライアントアプリケーションからサーバーアプリケーションに対して疎通確認（GET）およびファイルアップロード（POST）を実行できます。

また、上記の通信を実行すると、画面上に配置されたテキストエリアへ  
通信履歴（ログ）がリアルタイムに出力されます。

## 画面レイアウト

### クライアントアプリケーション
<img width="812" height="542" alt="image" src="https://github.com/user-attachments/assets/6cb734ae-49ba-496e-95e5-2728d1ad4d31" />

### サーバーアプリケーション
<img width="814" height="471" alt="image" src="https://github.com/user-attachments/assets/2fc43d83-2917-4207-8409-f38d0f8ef5ba" />

## 機能概要

- 疎通確認（GET）
- ファイルアップロード（POST）
- 認証方式の切り替え  
  - Basic 認証  
  - 匿名認証
- 通信ログのリアルタイム出力

## 技術スタック

### 使用技術

- C# (.NET 9.0)
- WPF
- Prism
- ReactiveProperty

### アーキテクチャ

- MVVM（Model / View / ViewModel）

## プロジェクト構成

- **View**  
  - 画面定義（XAML）

- **ViewModel**  
  - 画面状態およびユーザー操作の管理

- **Service**  
  - HttpClient：HTTP/HTTPS 通信処理  
  - Config：設定情報の読み書き  
  - Logger：通信ログの出力

## 関連リポジトリ

- [クライアントーション（HttpClientModule）](https://github.com/ssakaguchi/HttpClientModule)

## 注意点

- HTTPS 通信を行うため、サーバーアプリケーションは証明書のバインドが必要となり、管理者権限で起動する必要があります。
