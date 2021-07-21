# toio-cube-marker

<div align="center">
<img src="res/replay.gif" title="replay" alt="replay">
</div>

<br>

これは [**toio SDK for unity**](https://github.com/morikatron/toio-sdk-for-unity)（バージョン1.3.0）を利用したサンプルプログラムです。最大4人のプレイヤーが、一つのフィールド上に置かれた toio™コア キューブ（以降キューブ）を操縦し、色を塗る面積を競ったり、画像を掻き出してそこに描かれた内容を解答するゲームです。

#### ３つのゲームモード
- **`Battle`** ：キューブを走らせて色を塗り、指定した時間内で塗った面積を競い合う
- **`Image Quiz`** ：キューブを走らせることで、浮かび上がる画像の内容をあてるクイズゲームです。対戦側のチームがその邪魔をしてきます。(回答のチャンスは1回のみ)
- **`Multi-Image Quiz`** ：チーム毎に異なる画像を浮かび上がらせ、その内容をあてるクイズゲームです。(回答のチャンスは1回のみ)

#### 主な特徴
- シミュレータでも、リアルキューブでもゲームをホストすることが可能です；
- ゲーム画面を表示するモニター上に、簡易マットを敷くことで、リアルな「色塗り」を体験できます；
- **`Image Quiz`** と **`Multi-Image Quiz`** に使用する画像をカスタマイズできます；
- (開発者向け) インターフェイスを継承する形で、AI プレイヤーを簡単にカスタマイズできます；

> ※「簡易マット」の正式名称は「toio™コア キューブ（単体）付属の簡易プレイマット」です。

## セットアップ

### Unity プロジェクトのセットアップ

以下のパッケージを入手してインポートしてください。

- [**toio SDK for unity**](https://github.com/morikatron/toio-sdk-for-unity) (対応バージョン 1.3.0)
  - [【ドキュメント】](https://github.com/morikatron/toio-sdk-for-unity/blob/main/docs/download_sdk.md) に従ってセットアップしてください。
- **TextMeshPro**
  - プロジェクトを開くと「インポートしますか」のようなダイアログが表示されますので、そのダイアログでインポートボタンを押してください。
  - あるいは Package Manager で検索してインポートします。
- [**Photon 2 - Free**](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) (テスト済みバージョン 2.30.0)
  - Photon を使うには、AppId を取得して設定する必要があります、[【こちら】](https://doc.photonengine.com/en-us/pun/current/demos-and-tutorials/pun-basics-tutorial/intro#let_s_go__importing_pun_and_setup) を参考にしてください。
  - AppId の設定は Photon をインポートする際に表示されるダイアログの他、「Photon/PhotonUnityNetworking/Resources/PhotonServerSettings」で設定することもできます。
  - 上記「PhotonServerSettings」で、サーバー地域を指定することをおすすめします（日本の場合は「jp」）。指定しない場合は、アプリ実行時のネット環境によって違うサーバーに自動配分される可能性があります。
- [**Joystick Pack**](https://assetstore.unity.com/packages/tools/input-management/joystick-pack-107631) (テスト済みバージョン 2.1.0)
- [**WebGLInput**](https://github.com/kou-yeung/WebGLInput/releases) : WebGLのビルドで、コピー・ペーストを可能にするライブラリ
  - このライブラリを使わなくてもエラーは出ませんので、インストールしても、しなくてもかまいません。

### リアル環境のセットアップ

リアルの簡易マットで「色塗り」視覚体験を実現する方法を説明します。

必要なもの
- リアルキューブ 1~4 台
- 簡易マット （他のマットを利用するには、ソースコードを変更する必要があります）
- モニター

手順
- モニターを水平にします；
- モニターの上に（なるべく中央に）簡易マットを敷きます；
- 画面ミラーリングなどの機能を利用して、ゲーム画面をモニターに表示させます；
- アプリを起動し、キューブ接続・校正画面でリアルキューブを接続した後、校正を行い、モニター上表示されたマットとリアルの簡易マットとを一致させます；
- リモートの友達と対戦の場合は、ウェブカメラでリアル環境の映像を共有するのも良いです；
- ルーム作成してゲームを開始し、リアルのキューブと簡易マットに透けて見える「色/画像」を見ながら遊びます。


## 構成説明

本サンプルのシーンファイルは一つだけ（ /Assets/toio-cube-marker/Scenes/Scene.unity ）です。　　

シーンのヒエラルキーは以下になります。`Scripts` と `Canvas` それぞれに UI 以外のスクリプトと UI 関連のスクリプトが当てられています。

<div align="center">
<img src="res/hierarchy.png" title="hierarchy" alt="hierarchy">
</div>

以下簡単に `Scripts` の中身を説明します。
- Network (`NetworkManager.cs`)：Photon サーバーからの情報をまとめて持ちます。
- DuelCubeManager (`DuelCubeManager`)：シミュレータとリアル両方のキューブを管理します。
- Games：各ゲームの実装（ホストとクライアント両方）を持っています。
- ClientController：クライアント側の操作を取り入れるコンポーネントを持っています。
- HostAIControllers：ホスト側が作る AI プレイヤーを実装したコンポーネントを持っています。

本サンプルは、シーン遷移を使わずに、一つのシーンで複数の UI を切り替える方式を採用しています。
`Canvas` 下には各ページの UI が羅列されています。
- Canvas (`UICommon.cs`)：ページの切り替え機能を担当
  - PageTitle (`UITitle.cs`)：タイトル画面
  - PageLobby (`UILobby.cs`)：ルーム一覧
  - PageRoomCreate (`UIRoomCreate.cs`)：ルーム作成画面
  - PageConnect (`UIConnect.cs`)：リアルキューブを使用してルームを作成する場合、接続と校正を行う画面
  - PageRoom (`UIRoom.cs`)：ルーム画面
  - PageCustomQuiz (`UICustomQuiz.cs`)：Quiz や QuizDiff のゲームで使われる画像をカスタマイズする画面
  - Games (`UIGame.cs`)：各ゲームの共通 UI を管理
    - Common：バックグラウンド、フィルド、キューブマーカーなどの共通 UI
    - Battle/Quiz/QuizDiff (`UIGameBattle.cs` など)：ゲーム毎の UI を管理
    - Floating Joystick：ジョイスティック
    - Common：バックボタンなど、上層にある共通 UI
    - BattleOverlay/QuizOverlay/QuizDiffOverlay：結果表示などのゲーム毎の UI



#### ゲーム実装の構成

バトルゲームを例とします。

バトルの UI の有効化によって、`UIGameBattle.cs` がゲーム実装 `GameBattleHost.cs` `GameBattleClient.cs` の開始メソッドを呼び、バトルゲームが始まります。

ホストコード `GameBattleHost.cs` は
- `DuelCubeManager.cs` を利用し、キューブを用意してプレイヤーと関連付けます。
  - クライアントからの命令をキューブに送信
  - キューブの情報をクライアントに送信
- ゲームの進行を管理し、クライアントに通知
- HostAIControllers 上のコンポネントから生成した AI の行動をキューブに適用

クライアントコード `GameBattleClient.cs` は
- インターフェイス方式で、`UIGameBattle.cs` にマーカーを用意させてプレイヤーと関連付けます。
  - ホストからのキューブ情報をマーカーに反映
- ClientController 上のコンポーネントから読み取った操作情報を、ホストに送信
- クイズ回答などの情報をホストに送信

#### AI プレイヤーのカスタマイズ

`ConAIRandom.cs` のように、インターフェイス `IController` と `ControllerBase` を継承してクラスを実装すれば、カスタム AI プレイヤーを作れます。

自作の `IController` を利用する場合、`HostAIControllers` オブジェクトに、元のコンポーネントを削除して自作のスクリプトを追加すれば良いです。
ゲームでは、AI の数が最大3台になります。
また、AI プレイヤーは番号を持ち、`HostAIControllers` に追加された同じ順番の `IController` コンポーネントを実行します。
したがって、`HostAIControllers` には常に3つのコンポーネントを保持すると良いと思います。

キーボードやジョイスティックの操作を取り入れるクラスは、同じく `IController` を継承していますので、
AI プレイヤーを手動化、あるいはクライアントを AI 化するのも可能です。

## 注意事項

WebGL ビルドの場合、カスタムクイズで URL で画像をダウンロードすると CORS エラーが発生しますので、CORS 回避の対策が必要となります。

## 利用したもの

以下のものをご利用させていただきました。

- [Photon 2 - Free](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922)
- [Joystick Pack](https://assetstore.unity.com/packages/tools/input-management/joystick-pack-107631)
- [WebGLInput](https://github.com/kou-yeung/WebGLInput/releases)
- [源暎Nuゴシック](https://okoneya.jp/font/genei-nu-gothic.html) (Font)
- [いらすとや](https://www.irasutoya.com/) (クイズ用の画像素材)
