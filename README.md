# toio-cube-marker

<div align="center">
<img height=280 src="res" title="CubeMarker" alt="CubeMarker">
</div>

<br>

このサンプルは、最大4人がオンラインで一つのフィルド上、キューブを動かして色を塗ったり、画像を剥き出して内容物を当てたりして競争するゲームです。

### セットアップ

以下のパッケージを入手してインポートしてください。

- [**toio SDK for unity**](https://github.com/morikatron/toio-sdk-for-unity) (対応バージョン 1.3.0)
  - [【ドキュメント】](https://github.com/morikatron/toio-sdk-for-unity/blob/main/docs/download_sdk.md) に従ってセットアップしてください。
- **TextMesh Pro**
  - はじめてプロジェクトを開いてインポートを聞かれる際にインポートを行います。
  - あるいは Package Manager で検索してインポートします。
- [**Photon 2 - Free**](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) (テスト済みバージョン 2.30.0)
  - Photon を使うには、AppId を取得して設定する必要があります、[【こちら】](https://doc.photonengine.com/en-us/pun/current/demos-and-tutorials/pun-basics-tutorial/intro#let_s_go__importing_pun_and_setup) を参考にしてください。
- [**Joystick Pack**](https://assetstore.unity.com/packages/tools/input-management/joystick-pack-107631) (テスト済みバージョン 2.1.0)
- [**WebGLInput**](https://github.com/kou-yeung/WebGLInput/releases) : WebGLのビルドで、コピー・ペーストを可能にするもの
  - 使わなければインストールしなくてもエラーは出ないので選択可能です。

### 構成説明

シーンのヒエラルキーは以下になります。`Scripts` と `Canvas` それぞれに UI 以外のスクリプトと UI 関連のスクリプトが当てられています。

<div align="center">
<img src="res/hierarchy.png" title="hierarchy" alt="hierarchy">
</div>

`Scripts` の中身について説明しますと、
- Network (`NetworkManager.cs`)：Photon サーバーからの情報をまとめて持ちます。
- DuelCubeManager (`DuelCubeManager`)：シミュレータとリアル両方のキューブを管理します。
- Games：各ゲームの実装（ホストとクライエント両方）を持っています。
- ClientController：クライエント側の操作を取り入れるコンポネントを持っています。
- HostAIControllers：ホスト側が作る AI プレイヤーを実装したコンポネントを持っています。

本サンプルは、シーン遷移を使わずに、一つのシーンで複数の UI を切り替える方式を採用しています。
`Canvas` 下には各ページの UI が羅列されています。
- Canvas (`UICommon.cs`)：ページの切り替え機能を担当します。
  - PageTitle (`UITitle.cs`)：タイトル画面
  - PageLobby (`UILobby.cs`)：ルーム一覧
  - PageRoomCreate (`UIRoomCreate.cs`)：ルーム作成画面
  - PageConnect (`UIConnect.cs`)：リアルキューブを使用してルームを作成する場合、接続と校正を行う画面
  - PageRoom (`UIRoom.cs`)：ルーム画面
  - PageCustomQuiz (`UICustomQuiz.cs`)：Quiz や QuizDiff のゲームで使われる画像をカスタマイズする画面
  - Games (`UIGame.cs`)：各ゲームの共通 UI を管理します
    - Common：バックグラウンド、フィルド、キューブマーカーなどの共通 UI
    - Battle/Quiz/QuizDiff (`UIGameBattle.cs` など)：ゲーム毎の UI を管理します
    - Floating Joystick：ジョイスティック
    - Common：バックボタンなど、上層にある共通 UI
    - BattleOverlay/QuizOverlay/QuizDiffOverlay：結果表示などのゲーム毎の UI



#### ゲーム実装の構成

バトルゲームを例とします。

バトルの UI の有効化によって、`UIGameBattle.cs` がゲーム実装 `GameBattleHost.cs` `GameBattleClient.cs` の開始メソッドを呼び、バトルゲームが始まります。

ホストコード `GameBattleHost.cs` は
- `DuelCubeManager.cs` を利用し、キューブを用意してプレイヤーと関連付けます。
  - クライエントからの命令をキューブに送信
  - キューブの情報をクライエントに送信
- ゲームの進行を管理し、クライエントに通知
- HostAIControllers 上のコンポネントから生成した AI の行動をキューブに適用

クライエントコード `GameBattleClient.cs` は
- インターフェイス方式で、`UIGameBattle.cs` にマーカーを用意させてプレイヤーと関連付けます。
  - ホストからのキューブ情報をマーカーに反映
- ClientController 上のコンポネントから読み取った操作情報を、ホストに送信
- クイズ回答などの情報をホストに送信

#### AI プレイヤーのカスタマイズ

`ConAIRandom.cs` のように、インターフェイス `IController` と `ControllerBase` を継承してクラスを実装すれば、カスタム AI プレイヤーを作れます。

自作の `IController` を利用する場合、`HostAIControllers` オブジェクトに、元のコンポネントを削除して自作のスクリプトを追加すれば良いです。
ゲームでは、AI の数が最大3台になります。
また、AI プレイヤーは番号を持ち、`HostAIControllers` に追加された同じ順番の `IController` コンポネントを実行します。
なので、`HostAIControllers` には常に3つのコンポネントを持たすのをおすすめします。

キーボードやジョイスティックの操作を取り入れるクラスは、同じく `IController` を継承していますので、
AI プレイヤーを手動化、あるいはクライエントを AI 化するのもできます。

### 注意事項

WebGL ビルドの場合、カスタムクイズで URL で画像をダウンロードすると CORS エラーが発生しますので、CORS 回避の対策が必要となります。

### 利用したもの

以下のものをご利用させていただきました。

- [Photon 2 - Free](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922)
- [Joystick Pack](https://assetstore.unity.com/packages/tools/input-management/joystick-pack-107631)
- [WebGLInput](https://github.com/kou-yeung/WebGLInput/releases)
- [源暎Nuゴシック](https://okoneya.jp/font/genei-nu-gothic.html) (Font)
- [いらすとや](https://www.irasutoya.com/) (クイズ用の画像素材)
