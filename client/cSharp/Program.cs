// U16旭川プログラミングコンテスト実行委員会　下村
// 大会対戦用Bot
// 
// 2015.10.23 敵との遭遇した時のバグフィックス
// 2015.10.27 Lookの戻り値を勘違いしてたミスの修正
// 2016.7.15 第6回U-16大会用に変更
// 変更点
//  敵を見つけたら逃げるようにする。偶然プットの可能性を低く
//  アイテムは20ターン毎でひとつ取得するようにする。
//  同じ場所を行かないよう移動アルゴリズムをちょっとだけ賢く
//
// 2016.9.12 移動および強さの調整（v2公開バージョン）
// 2018.9.2 第8回U-16大会用に変更
// 変更点
//  1ターン目の移動が固定されていたものを行ける方向に乱数で行くことにした
//  try catchによる例外処理の追加
//  Lookで敵を見つけた場合35%の確率で逃げ忘れることにした。
//  XMLドキュメントの追加
//  MSコード分析

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using CHaser;

namespace u16asahikawaBotv2
{
    /// <summary>
    /// 命令一覧
    /// </summary>
    public enum Funcs
    {
        None,
        WalkRight,
        WalkLeft,
        WalkUp,
        WalkDown,
        LookRight,
        LookLeft,
        LookUp,
        LookDown,
        SearchRight,
        SearchLeft,
        SearchUp,
        SearchDown,
        PutRight,
        PutLeft,
        PutUp,
        PutDown,
    };

    /// <summary>
    /// メインクラス
    /// </summary>
    class Program
    {
        const int nCOOL = 40000, nHOT = 50000;
        const int oCOOL = 2009, oHOT = 2010;

        const int Floor = 0, Enemy = 1, Block = 2, Item = 3;

        // 接続用インスタンス
        private static Client Target = Client.Create();
        //private static Client Target = new Client(oHOT,"botA");

        // マップ記録用ハッシュテーブル
        private static Dictionary<Point, int> map = new Dictionary<Point, int>();
        private static int seed = Environment.TickCount;

        /// <summary>
        /// メインメソッド
        /// </summary>
        /// <param name="args">未使用</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        static void Main(string[] args)
        {
            // 初期値
            int turn = 0, stepCount = 0, itemCount = 0;
            int hotoke_face = 0;

            bool safe = true;
            Point pos = new Point();// 初期座標は0,0
            List<Funcs> cmdList = new List<Funcs>();// 発行したコマンド記録
            List<Point> posList = new List<Point>();// 過去の位置記録
            // 移動経過記録
            posList.Add(pos);
            List<int[]> actionValue = new List<int[]>();// コマンド発行後の戻り値記録

            Random rnd;

            while (true)
            {
                stepCount++;
                turn++;

                Funcs cmd = new Funcs();
                int[] value = Target.Ready();

                mapAdd(pos, value);

                rnd = new Random(seed++);

                // 移動可能箇所の列挙
                var movableLocation = new List<Funcs>() { Funcs.WalkLeft, Funcs.WalkUp, Funcs.WalkRight, Funcs.WalkDown, };

                #region 周囲に敵がいるかチェック
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == Enemy)
                    {
                        if (i == 1 || i == 3 || i == 5 || i == 7)
                        {
                            hotoke_face++;
                            Console.WriteLine("仏の顔={0}", hotoke_face);

                            // 仏の顔も三度まで
                            if (hotoke_face > 3) func(enemyCheck(value)); // 即殺す、終了
                        }
                        movableLocation = enemyLocCalcRemoveList(i, movableLocation);
                    }
                }
                #endregion

                // 周囲情報から移動対象外を見つける
                for (int i = 1; i < 8; i += 2)
                {
                    if (value[i] == Block)
                    {
                        if (i == 1 && movableLocation.Contains(Funcs.WalkUp)) movableLocation.Remove(Funcs.WalkUp);
                        if (i == 3 && movableLocation.Contains(Funcs.WalkLeft)) movableLocation.Remove(Funcs.WalkLeft);
                        if (i == 5 && movableLocation.Contains(Funcs.WalkRight)) movableLocation.Remove(Funcs.WalkRight);
                        if (i == 7 && movableLocation.Contains(Funcs.WalkDown)) movableLocation.Remove(Funcs.WalkDown);
                    }
                }


                #region アイテムを取っていいかどうかの計算、20ターンで１個取得、ダメなら移動候補から消去
                if (itemCount * 20 + 20 >= turn) //まだアイテムを取れない
                {
                    // 削除予定移動命令候補
                    List<Funcs> removeCandidateList = new List<Funcs>();

                    // 上下左右の値が欲しいので2づつインクリメント
                    for (int i = 1; i < 8; i += 2)
                    {
                        if (value[i] == Item)
                        {
                            if (i == 1) removeCandidateList.Add(Funcs.WalkUp);
                            else if (i == 3) removeCandidateList.Add(Funcs.WalkLeft);
                            else if (i == 5) removeCandidateList.Add(Funcs.WalkRight);
                            else if (i == 7) removeCandidateList.Add(Funcs.WalkDown);
                            else
                            {
                                // ※何も行わない(あり得ないが)
                            }
                        }
                    }
                    // 行ける方向が全てアイテムだった場合乱数で１箇所は残す
                    if (movableLocation.Count == removeCandidateList.Count)
                    {
                        removeCandidateList.Remove(removeCandidateList[rnd.Next(removeCandidateList.Count)]);
                    }
                    foreach (Funcs remove in removeCandidateList)
                    {
                        movableLocation.Remove(remove);
                    }
                }
                #endregion

                #region 基本動作
                try
                {
                    switch (turn)
                    {
                        case 1:
                            cmd = movableLocation[rnd.Next(movableLocation.Count)];
                            break;

                        default:
                            // 前のコマンド方向をwalkに変換
                            Funcs cmdListLast = cmdList.Last();
                            cmd = any2walk(cmdListLast);

                            // 前回のアクションがLookで返り値に敵がいた場合その方向は除外する。put負けしやすく 30%の確率で逃げ忘れる
                            if (rnd.Next() % 100 < 70)
                            {
                                if (movableLocation.Count > 1 && (cmdListLast > Funcs.WalkDown && cmdListLast < Funcs.SearchRight) && safe)
                                {
                                    int[] actionValueLast = actionValue.Last();
                                    foreach (int val in actionValueLast)
                                    {
                                        if (val == Enemy)
                                        {
                                            Funcs lookToWalk = any2walk(cmdListLast);
                                            if (movableLocation.Contains(lookToWalk)) movableLocation.Remove(lookToWalk);
                                            safe = false;
                                            stepCount = -1;
                                        }
                                    }
                                }
                            }
                            // 進行方向が壁か３歩後もしくは取ってはいけないアイテムなら方向転換とLook
                            if (!movableLocation.Contains(cmd) || stepCount % 4 == 0)
                            {
                                stepCount = 0;

                                if (movableLocation.Count() <= 1)
                                {
                                    if (movableLocation.Count() == 1) cmd = walk2look(movableLocation.First());
                                    else cmd = Funcs.None; //行ける場所なし
                                }
                                else // 移動可能方向が２方向以上
                                {
                                    // 来た方向が消せるなら消す
                                    Funcs reverceD = reverceDirection(cmdListLast);
                                    if (movableLocation.Count > 1 && movableLocation.Contains(reverceD))
                                    {
                                        movableLocation.Remove(reverceD);
                                    }

                                    // 残った移動選択肢の中からひとつ削る
                                    if (movableLocation.Count > 1)
                                    {
                                        //計算と乱数を半々にする
                                        if (turn % 2 != 0) movableLocation.Remove(calcRemove(movableLocation, pos));
                                        else movableLocation.Remove(movableLocation[rnd.Next(movableLocation.Count)]);
                                    }

                                    // 残ったものが複数あるばあい乱数で移動方向を決める
                                    if (movableLocation.Count > 1)
                                    {
                                        cmd = movableLocation[rnd.Next(movableLocation.Count)];
                                    }
                                    else cmd = movableLocation.First();

                                    // 敵がそばにいる場合はLookしないで即移動
                                    if (safe) cmd = walk2look(cmd);
                                    else safe = true;
                                }
                            }

                            break;
                    }
                    #endregion
                }
                catch (Exception) { }

                // 移動先がアイテムならカウント
                if (move2GetItem(cmd, value)) itemCount++;

                // None命令等をチェック
                cmd = cmdNoneCheck(cmd);
                Console.WriteLine("*** {0} ***", cmd);
                // 実際のアクション
                value = func(cmd);

                // コマンド実行後の次の位置の計算
                pos = calcPos(posList.Last(), cmd, 1);

                // マップの更新
                mapAdd(pos, cmd, value);

                // cmdの追加
                cmdList.Add(cmd);
                posList.Add(pos);
                actionValue.Add(value);
            }
        }

        /// <summary>
        /// 移動先がアイテムかチェック
        /// </summary>
        /// <param name="cmd">移動予定命令</param>
        /// <param name="value">周囲のタイル</param>
        /// <returns>アイテム有無</returns>
        private static bool move2GetItem(Funcs cmd, int[] value)
        {
            bool result = false;
            if (cmd == Funcs.WalkUp && value[1] == Item) result = true;
            if (cmd == Funcs.WalkDown && value[7] == Item) result = true;
            if (cmd == Funcs.WalkRight && value[5] == Item) result = true;
            if (cmd == Funcs.WalkLeft && value[3] == Item) result = true;
            return result;
        }

        /// <summary>
        /// 敵と遭遇した場合行っては行けない場所を除外する
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="movableLocation">命令一覧</param>
        /// <returns>実行可能命令List</returns>
        private static List<Funcs> enemyLocCalcRemoveList(int direction, List<Funcs> movableLocation)
        {
            // 敵のいる方向は消去する。
            switch(direction)
            {
                case 0:
                    movableLocation.Remove(Funcs.WalkLeft);
                    movableLocation.Remove(Funcs.WalkUp);
                    break;
                case 1:
                    movableLocation.Remove(Funcs.WalkUp);
                    break;
                case 2:
                    movableLocation.Remove(Funcs.WalkUp);
                    movableLocation.Remove(Funcs.WalkRight);
                    break;
                case 3:
                    movableLocation.Remove(Funcs.WalkLeft);
                    break;
                case 4: // 敵が乗っかってる？
                    movableLocation.Remove(Funcs.WalkRight);
                    movableLocation.Remove(Funcs.WalkUp);
                    movableLocation.Remove(Funcs.WalkLeft);
                    movableLocation.Remove(Funcs.WalkDown);
                    break;
                case 5:
                    movableLocation.Remove(Funcs.WalkLeft);
                    break;
                case 6:
                    movableLocation.Remove(Funcs.WalkLeft);
                    movableLocation.Remove(Funcs.WalkDown);
                    break;
                case 7:
                    movableLocation.Remove(Funcs.WalkDown);
                    break;
                case 8:
                    movableLocation.Remove(Funcs.WalkDown);
                    movableLocation.Remove(Funcs.WalkRight);
                    break;
                default:
                    break;
            }

            return movableLocation;
        }

        /// <summary>
        /// マップを更新する
        /// </summary>
        /// <param name="pos">マップ情報</param>
        /// <param name="func">実行した命令</param>
        /// <param name="value"></param>
        private static void mapAdd(Point pos, Funcs func, int[] value)
        {
            Point[] offsetPoint;

            switch(func)
            {
                case Funcs.LookUp:
                    offsetPoint = new Point[9]{new Point(-1,-3), new Point(0,-3), new Point(1,-3),
                                               new Point(-1,-2), new Point(0,-2), new Point(1,-2),
                                               new Point(-1,-1), new Point(0,-1), new Point(1,-1),};
                    break;
                case Funcs.LookDown:
                    offsetPoint = new Point[9]{new Point(-1,1), new Point(0,1), new Point(1,1),
                                               new Point(-1,2), new Point(0,2), new Point(1,2),
                                               new Point(-1,3), new Point(0,3), new Point(1,3),};
                    break;
                case Funcs.LookRight:
                    offsetPoint = new Point[9]{new Point(1,-1), new Point(2,-1), new Point(3,-1),
                                               new Point(1,0),  new Point(2,0),  new Point(3,0),
                                               new Point(1,1),  new Point(2,1),  new Point(3,1),};
                    break;
                case Funcs.LookLeft:
                    offsetPoint = new Point[9]{new Point(-3,-1), new Point(-2,-1), new Point(-1,-1),
                                               new Point(-3,0),  new Point(-2,0),  new Point(-1,0),
                                               new Point(-3,1),  new Point(-2,1),  new Point(-1,1),};
                    break;
                case Funcs.SearchUp:
                    offsetPoint = new Point[9]{new Point(0,-1), new Point(0,-2), new Point(0,-3),
                                               new Point(0,-4), new Point(0,-5), new Point(0,-6),
                                               new Point(0,-7), new Point(0,-8), new Point(0,-9),};
                    break;
                case Funcs.SearchDown:
                    offsetPoint = new Point[9]{new Point(0,1), new Point(0,2), new Point(0,3),
                                               new Point(0,4), new Point(0,5), new Point(0,6),
                                               new Point(0,7), new Point(0,8), new Point(0,9),};
                    break;
                case Funcs.SearchRight:
                    offsetPoint = new Point[9]{new Point(1,0), new Point(2,0), new Point(3,0),
                                               new Point(4,0), new Point(5,0), new Point(6,0),
                                               new Point(7,0), new Point(8,0), new Point(9,0),};
                    break;
                case Funcs.SearchLeft:
                    offsetPoint = new Point[9]{new Point(-1,0), new Point(-2,0), new Point(-3,0),
                                               new Point(-4,0), new Point(-5,0), new Point(-6,0),
                                               new Point(-7,0), new Point(-8,0), new Point(-9,0),};

                    break;
                case Funcs.WalkUp:
                case Funcs.WalkDown:
                case Funcs.WalkRight:
                case Funcs.WalkLeft:
                case Funcs.PutUp:
                case Funcs.PutDown:
                case Funcs.PutRight:
                case Funcs.PutLeft:
                default:
                    offsetPoint = new Point[9]{new Point(-1,-1), new Point(0,-1), new Point(1,-1),
                                               new Point(-1,0),  new Point(0,0),  new Point(1,0),
                                               new Point(-1,1),  new Point(0,1),  new Point(1,1),};
                    break;
            }
            // マップディクショナリの更新
            for (int i = 0; i < offsetPoint.Length; i++)
            {
                map[new Point(pos.X + offsetPoint[i].X, pos.Y + offsetPoint[i].Y)] = value[i];
            }

        }

        /// <summary>
        /// GetReday用マップ更新
        /// </summary>
        /// <param name="pos">マップ情報</param>
        /// <param name="value"></param>
        private static void mapAdd(Point pos, int[] value)
        {
            Point[] offsetPoint = new Point[9]{new Point(-1,-1), new Point(0,-1), new Point(1,-1),
                                               new Point(-1,0),  new Point(0,0),  new Point(1,0),
                                               new Point(-1,1),  new Point(0,1),  new Point(1,1),};
            Console.WriteLine(map);
            for (int i = 0; i < offsetPoint.Length; i++)
            {
                map[new Point(pos.X + offsetPoint[i].X, pos.Y + offsetPoint[i].Y)] = value[i];
            }
        }

        /// <summary>
        /// 行かなくても良さそうな方向をひとつ計算する
        /// </summary>
        /// <param name="movableLocation"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static Funcs calcRemove(List<Funcs> movableLocation, Point pos)
        {
            Funcs function = new Funcs();
            int minScore = int.MaxValue;

            for(int i = 0; i<movableLocation.Count; i++)
            {
                int score = 0;
                const int distance = 2;   //2歩進んだ先の周囲を探索
                Point targetPos = new Point();
                switch(movableLocation[i])
                {
                    case Funcs.WalkRight:
                        targetPos = new Point(pos.X + distance, pos.Y);
                        break;
                    case Funcs.WalkLeft:
                        targetPos = new Point(pos.X - distance, pos.Y);
                        break;
                    case Funcs.WalkUp:
                        targetPos = new Point(pos.X, pos.Y - distance);
                        break;
                    case Funcs.WalkDown:
                        targetPos = new Point(pos.X , pos.Y + distance);
                        break;
                }
                // 目的地がどうなってるか知ってるか？
                if (map.ContainsKey(targetPos))
                {
                    if (map[targetPos] == Block) score -= 2; //目的地がブロックなら行く候補として魅力薄
                }

                // 目的地の周囲情報のオフセット位置
                Point[] arroundPoint = new Point[9]{new Point(-1,-1), new Point(0,-1), new Point(1,-1),
                                                    new Point(-1,0),  new Point(0,0),  new Point(1,0),
                                                    new Point(-1,1),  new Point(0,1),  new Point(1,1),};

                foreach (Point p in arroundPoint)
                {
                    Point scanPos = new Point(targetPos.X + p.X, targetPos.Y + p.Y);
                    if (map.ContainsKey(scanPos))
                    {
                        if (map[p] == Block) score -= 2; //ブロックなら評価下げる
                        else score -= 1;
                    }
                }
                if (minScore > score)
                {
                    minScore = score;
                    function = movableLocation[i];
                }
                // スコア一緒なら乱数でどっちを採用するか決める
                else if(minScore == score)
                {
                    Random rnd = new Random(seed++);
                    if(rnd.Next(2) != 0) function = movableLocation[i];
                }
            }
            // ダメそうなやつを返す
            return function;
        }

        /// <summary>
        /// Walkコマンドによって次の自位置を計算する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="cmd"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static Point calcPos(Point pos, Funcs cmd, int distance)
        {
            if (cmd == Funcs.WalkUp) pos.Y-=distance;
            if (cmd == Funcs.WalkDown) pos.Y+=distance;
            if (cmd == Funcs.WalkRight) pos.X+= distance;
            if (cmd == Funcs.WalkLeft) pos.X-=distance;
            return pos;
        }

        /// <summary>
        /// 来た方向を計算する
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private static Funcs reverceDirection(Funcs cmd)
        {
            Funcs ret = new Funcs();
            if (cmd == Funcs.WalkUp) ret = Funcs.WalkDown;
            if (cmd == Funcs.WalkDown) ret = Funcs.WalkUp;
            if (cmd == Funcs.WalkRight) ret = Funcs.WalkLeft;
            if (cmd == Funcs.WalkLeft) ret = Funcs.WalkRight;
            return ret;
        }

        /// <summary>
        /// 移動命令をLook命令に変更する
        /// </summary>
        /// <param name="cmd">実行予定命令</param>
        /// <returns>Look命令</returns>
        private static Funcs walk2look(Funcs cmd)
        {
            return cmd + 4;
        }

        /// <summary>
        /// 移動以外の命令を移動命令に変換する
        /// </summary>
        /// <param name="cmd">実行予定命令</param>
        /// <returns>移動命令</returns>
        private static Funcs any2walk(Funcs cmd)
        {
            if (cmd > Funcs.SearchDown) cmd -= 12; //put
            else if (cmd > Funcs.LookDown) cmd -= 8;//Search
            else if (cmd > Funcs.WalkDown) cmd -= 4;//Look

            return cmd;
        }

        /// <summary>
        /// 敵と上下左右で出会った時の殺す命令
        /// </summary>
        /// <param name="value">周囲の状況</param>
        /// <returns>実行予定命令</returns>
        private static Funcs enemyCheck(int[] value)
        {
            Funcs action = Funcs.None;
            if (value[1] == 1) action = Funcs.PutUp;
            if (value[3] == 1) action = Funcs.PutLeft;
            if (value[5] == 1) action = Funcs.PutRight;
            if (value[7] == 1) action = Funcs.PutDown;

            return action;
        }

        /// <summary>
        /// None命令ならLookかSearchどれかをランダムに発行する。
        /// </summary>
        /// <param name="data">実行予定命令</param>
        /// <returns>
        /// None：ランダムに選ばれた探査命令
        /// 他：引数をそのまま返す
        /// </returns>
        private static Funcs cmdNoneCheck(Funcs data)
        {
            if(data == Funcs.None || data > Funcs.PutDown)
            {
                Random rnd = new Random(seed++);

                data = (Funcs)rnd.Next(5, 13);// 5=LookRight, 12=SearchDown

                Console.WriteLine("None命令が発行されましたので{0}を実行しました", data);
            }
            return data;
        }

        /// <summary>
        /// アクション
        /// </summary>
        /// <param name="data">実行予定命令構造体</param>
        /// <returns>実行命令</returns>
        private static int[] func(Funcs data)
        {
            int[] result = new int[9];
            switch (data)
            {
                case Funcs.WalkRight:
                    result = Target.WalkRight();
                    break;
                case Funcs.WalkLeft:
                    result = Target.WalkLeft();
                    break;
                case Funcs.WalkUp:
                    result = Target.WalkUp();
                    break;
                case Funcs.WalkDown:
                    result = Target.WalkDown();
                    break;
                case Funcs.LookRight:
                    result = Target.LookRight();
                    break;
                case Funcs.LookLeft:
                    result = Target.LookLeft();
                    break;
                case Funcs.LookUp:
                    result = Target.LookUp();
                    break;
                case Funcs.LookDown:
                    result = Target.LookDown();
                    break;
                case Funcs.SearchRight:
                    result = Target.SearchRight();
                    break;
                case Funcs.SearchLeft:
                    result = Target.SearchLeft();
                    break;
                case Funcs.SearchUp:
                    result = Target.SearchUp();
                    break;
                case Funcs.SearchDown:
                    result = Target.SearchDown();
                    break;
                case Funcs.PutRight:
                    result = Target.PutRight();
                    break;
                case Funcs.PutLeft:
                    result = Target.PutLeft();
                    break;
                case Funcs.PutUp:
                    result = Target.PutUp();
                    break;
                case Funcs.PutDown:
                    result = Target.PutDown();
                    break;
                default:
                    result = Target.SearchUp(); // あり得ないが
                    break;
            }
            return result;
        }
    }
}
