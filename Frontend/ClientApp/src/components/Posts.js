import React, { Component } from 'react';
import Masonry from 'react-masonry-infinite';
import Utils from "../Utils";

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { posts: [], loading: true, hasMore: false };

    this.expectedLoads = 0

    this.handleClick = this.handleClick.bind(this);
    this.loadMore = this.loadMore.bind(this);
    this.imageReady = this.imageReady.bind(this);

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
    .then(response => response.json())
    .then(data => {
      this.setState({ posts: data, loading: false });
    });
  }
  
  loadMore() {}

  handleClick(e) {
    var blogname = this.props.match.params.blogname;
    var id = e.target.getAttribute('data-id');
    this.props.history.push('/post/' + blogname + "/" + id);
  }

  imageReady() {
    // expectedLoads--;
    // if (this.expectedLoads == 0) {
    //   this.masonryGrid.forcePack();
    // }
    this.masonryGrid.forcePack();
  }

  renderPostsTable(posts) {
    return (
      <Masonry className="masonry" hasMore={this.state.hasMore} loadMore={this.loadMore} ref={(child) => { this.masonryGrid = child; }}
        sizes={[
          { columns: 1, gutter: 10 },
          { mq: '820px', columns: 2, gutter: 10 },
          { mq: '1145px', columns: 3, gutter: 10 },
          { mq: '1470px', columns: 4, gutter: 10 },
          { mq: '1795px', columns: 5, gutter: 10 },
        ]}
      >
        {posts.map(post =>
          <div key={post.Id}>
            {(!post.Photos || post.Photos.length === 0) && 
              <span>No photo</span>
            }
            {(post.Photos && post.Photos.length !== 0) &&
              <div>
                <img src={Utils.GetSmallPhotoUrl(post)} width="250" data-id={post.Id} onClick={this.handleClick} onLoad={this.imageReady} onError={this.imageReady} alt=""/>
              </div>
            }
          </div>
        )}
      </Masonry>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.renderPostsTable(this.state.posts);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
      </div>
    );
  }
}
